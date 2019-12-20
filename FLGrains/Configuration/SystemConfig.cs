using Cassandra;
using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrains.ServiceInterfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans;
using Orleans.Concurrency;
using OrleansCassandraUtils.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FLGrains.Configuration
{
    public class SystemConfig : Grain, ISystemConfig
    {
        class PrivateAccessorContractResolver : DefaultContractResolver
        {
            public static JsonSerializerSettings SerializerSettings { get; } = new JsonSerializerSettings
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = new PrivateAccessorContractResolver()
            };


            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var result = base.CreateProperty(member, memberSerialization);

                if (!result.Writable && member is PropertyInfo property)
                    result.Writable = property.GetSetMethod(true) != null;

                return result;
            }
        }


        ConfigData? data;
        readonly ISystemSettingsProvider systemSettingsProvider;


        public SystemConfig(ISystemSettingsProvider systemSettingsProvider)
        {
            this.systemSettingsProvider = systemSettingsProvider;
        }

        public override async Task OnActivateAsync()
        {
            DelayDeactivation(TimeSpan.MaxValue);

            await InternalUpdateConfigFromDatabase();

            await base.OnActivateAsync();
        }

        public Task<Immutable<ConfigData>> GetConfig()
        {
            return Task.FromResult(GetData().AsImmutable());
        }

        private ConfigData GetData() => data ?? throw new Exception("Config data not initialized yet");

        static ConfigData ParseConfigData(string data) => JsonConvert.DeserializeObject<ConfigData>(data, PrivateAccessorContractResolver.SerializerSettings);

        static async Task<ConfigData> ReadConfigDataFromDatabase(ISession session, Queries queries)
        {
            var rows = await session.ExecuteAsync(queries["fl_readConfig"].Bind());
            var data = Convert.ToString(rows.FirstOrDefault()?["data"]);

            if (!string.IsNullOrEmpty(data))
                return ParseConfigData(data);
            else
                return new ConfigData();
        }

        static async Task<List<GroupConfig>> ReadGroupsFromDatabase(ISession session, Queries queries)
        {
            var rows = await session.ExecuteAsync(queries["fl_readGroups"].Bind());

            var result = new List<GroupConfig>();

            foreach (var row in rows)
                result.Add(new GroupConfig(
                    Convert.ToUInt16(row["id"]),
                    Convert.ToString(row["name"])
                    ));

            return result;
        }

        //!!
        /* This can be improved by reading the categories from the database on each silo.
         * We could do quorom writes and reads to make sure everybody gets the latest data.
         * Once we have a way to change the data at runtime, we should add a way to push
         * incremental updates to other silos. All of this is unlikely to impact performance
         * unless we have hundreds of megabytes of data here though.
         */
        static async Task<List<CategoryConfig>> ReadCategoriesFromDatabase(ISession session, Queries queries, IEnumerable<GroupConfig> groups)
        {
            var groupsByID = groups.ToDictionary(g => g.ID);

            var rows = await session.ExecuteAsync(queries["fl_readCategories"].Bind());

            var result = new List<CategoryConfig>();

            foreach (var row in rows)
                result.Add(new CategoryConfig(
                    Convert.ToString(row["name"]),
                    ((IDictionary<string, IEnumerable<string>>)row["words"])
                        .Select(kv => new CategoryConfig.Entry(kv.Key, kv.Value)),
                    groupsByID[Convert.ToUInt16(row["group_id"])]
                    ));

            return result;
        }

        async Task InternalUpdateConfigFromDatabase()
        {
            var connectionString = systemSettingsProvider.Settings.Values.ConnectionString;
            var session = await CassandraSessionFactory.CreateSession(connectionString);
            var queries = await Queries.CreateInstance(session);

            var newData = await ReadConfigDataFromDatabase(session, queries);
            newData.Groups = await ReadGroupsFromDatabase(session, queries);
            newData.Categories = await ReadCategoriesFromDatabase(session, queries, newData.Groups);

            SetNewData(newData);
        }

        void SetNewData(ConfigData newData)
        {
            newData.Version = (data?.Version ?? 0) + 1;
            data = newData;
        }

        Task PushUpdateToAllSilos() => GrainFactory.GetGrain<IConfigUpdaterGrain>(0).PushUpdateToAllSilos(GetData().Version);

        public async Task UpdateConfigFromDatabase()
        {
            await InternalUpdateConfigFromDatabase();

            await PushUpdateToAllSilos();
        }

        public async Task UploadConfig(string jsonConfig)
        {
            var connectionString = systemSettingsProvider.Settings.Values.ConnectionString;
            var session = await CassandraSessionFactory.CreateSession(connectionString);

            var data = GetData();
            var newData = ParseConfigData(jsonConfig);
            newData.Categories = data.Categories;
            newData.Groups = data.Groups;

            var statement = await session.PrepareAsync("update fl_config set data = :data where key = 0;");
            statement.SetConsistencyLevel(ConsistencyLevel.EachQuorum);
            await session.ExecuteAsync(statement.Bind(new { data = jsonConfig }));

            SetNewData(newData);

            await PushUpdateToAllSilos();
        }
    }
}
