using Cassandra;
using FLGrainInterfaces;
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

namespace FLGrains
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


        ConfigData data;
        IConnectionStringProvider connectionStringProvider;


        public SystemConfig(IConnectionStringProvider connectionStringProvider)
        {
            this.connectionStringProvider = connectionStringProvider;
        }

        public override async Task OnActivateAsync()
        {
            DelayDeactivation(TimeSpan.MaxValue);

            await InternalUpdateConfigFromDatabase();

            await base.OnActivateAsync();
        }

        public Task<Immutable<ConfigData>> GetConfig()
        {
            return Task.FromResult(data.AsImmutable());
        }

        //?? validate
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

        //?? This is not the greatest idea. We should be able to push incremental updates to silos,
        //   since this data is likely to be some hundreds of megabytes. Each silo could read from
        //   the database (not likely to change anything), and we should be able to push *incremental*
        //   updates to silos when something changes. We could have a version, and expect to receive
        //   sequential versions on the silos, asking for all the data if it goes out of sync.
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
            var connectionString = connectionStringProvider.ConnectionString;
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

        Task PushUpdateToAllSilos() => GrainFactory.GetGrain<IConfigUpdaterGrain>(0).PushUpdateToAllSilos(data.Version);

        public async Task UpdateConfigFromDatabase()
        {
            await InternalUpdateConfigFromDatabase();

            await PushUpdateToAllSilos();
        }

        public async Task UploadConfig(string jsonConfig)
        {
            var connectionString = connectionStringProvider.ConnectionString;
            var session = await CassandraSessionFactory.CreateSession(connectionString);

            var newData = ParseConfigData(jsonConfig);

            var statement = await session.PrepareAsync("update fl_config set data = :data where key = 0;");
            statement.SetConsistencyLevel(ConsistencyLevel.EachQuorum);
            await session.ExecuteAsync(statement.Bind(new { data = jsonConfig }));

            SetNewData(newData);

            await PushUpdateToAllSilos();
        }
    }
}
