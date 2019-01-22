using FLGrainInterfaces;
using Cassandra;
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
using FLGrains.ServiceInterfaces;

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

        static async Task<ConfigData> ReadConfigDataFromDatabase(ISession session)
        {
            var rows = await session.ExecuteAsync(new SimpleStatement("select data from fl_config where key = 0"));
            var data = Convert.ToString(rows.FirstOrDefault()?["data"]);

            if (!string.IsNullOrEmpty(data))
                return ParseConfigData(data);
            else
                return new ConfigData();
        }

        async Task InternalUpdateConfigFromDatabase()
        {
            var connectionString = connectionStringProvider.ConnectionString;
            var session = await CassandraSessionFactory.CreateSession(connectionString);

            var newData = await ReadConfigDataFromDatabase(session);

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
