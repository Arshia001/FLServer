#pragma warning disable IDE0037 // Use inferred member name

using Cassandra;
using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrains.ServiceInterfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans;
using Orleans.Concurrency;
using OrleansCassandraUtils.Utils;
using System;
using System.Collections.Generic;
using System.IO;
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
        readonly ILogger logger;

        public SystemConfig(ISystemSettingsProvider systemSettingsProvider, ILogger<SystemConfig> logger)
        {
            this.systemSettingsProvider = systemSettingsProvider;
            this.logger = logger;
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

        static T ParseJson<T>(string data) => JsonConvert.DeserializeObject<T>(data, PrivateAccessorContractResolver.SerializerSettings);

        static ConfigData ParseConfigData(string data) => ParseJson<ConfigData>(data);

        static async Task<string> ReadDatabaseConfigEntry(ISession session, Queries queries, string key)
        {
            var rows = await session.ExecuteAsync(queries["fl_readConfig"].Bind(new { key = key }));
            return Convert.ToString(rows.FirstOrDefault()?["data"])!;
        }

        static Task WriteDatabaseConfigEntry(ISession session, Queries queries, string key, string value) =>
            session.ExecuteAsync(queries["fl_updateConfig"].Bind(new { data = value, key = key }));

        static async Task<ConfigData> ReadConfigDataFromDatabase(ISession session, Queries queries)
        {
            var data = await ReadDatabaseConfigEntry(session, queries, "config");

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
                    Convert.ToString(row["name"])!
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
        async Task<List<CategoryConfig>> ReadCategoriesFromDatabase(ISession session, Queries queries, IEnumerable<GroupConfig> groups)
        {
            var groupsByID = groups.ToDictionary(g => g.ID);

            var rows = await session.ExecuteAsync(queries["fl_readCategories"].Bind());

            var result = new List<CategoryConfig>();

            foreach (var row in rows)
                try
                {
                    if (row["words"] == null || row["name"] == null)
                    {
                        logger.LogError($"Found category with no words or name: {row["name"]}");
                        continue;
                    }

                    var name = Convert.ToString(row["name"])!;

                    var groupID = Convert.ToUInt16(row["group_id"]);
                    if (!groupsByID.TryGetValue(groupID, out var group))
                        throw new Exception($"Found category {name} with unknown group id {groupID}");

                    var words = ((IDictionary<string, IEnumerable<string>>)row["words"]).Select(kv => new CategoryConfig.Entry(kv.Key, kv.Value));

                    result.Add(new CategoryConfig(name, words, group));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to read row with key {row["name"]}", ex);
                }

            return result;
        }

        async Task<List<RenamedCategoryConfig>> ReadRenamedCategoriesFromDatabase(ISession session, Queries queries)
        {
            var rows = await session.ExecuteAsync(queries["fl_readRenamedCategories"].Bind());

            var result = new List<RenamedCategoryConfig>();

            foreach (var row in rows)
            {
                var oldName = row["old_name"] as string;
                if (string.IsNullOrEmpty(oldName))
                    throw new Exception("Found renamed category with no old name");

                var newName = row["new_name"] as string;
                if (string.IsNullOrEmpty(newName))
                    throw new Exception("Found renamed category with no new name");

                result.Add(new RenamedCategoryConfig(oldName, newName));
            }

            return result;
        }

        async Task<(
                List<GroupConfig>, 
                List<CategoryConfig>,
                List<RenamedCategoryConfig>,
                uint latestClientVersion,
                uint lastCompatibleClientVersion,
                AvatarConfig,
                List<BotConfig>
            )>
            ReadNonJsonConfigDataFromDatabase(ISession session, Queries queries)
        {
            var groups = await ReadGroupsFromDatabase(session, queries);
            return (
                groups,
                await ReadCategoriesFromDatabase(session, queries, groups),
                await ReadRenamedCategoriesFromDatabase(session, queries),
                await ReadClientVersionFromDatabase(session, queries, "latest-version"),
                await ReadClientVersionFromDatabase(session, queries, "last-compatible-version"),
                await ReadAvatarFromDatabase(session, queries),
                await ReadBotsFromDatabase(session, queries)
            );
        }

        async Task<uint> ReadClientVersionFromDatabase(ISession session, Queries queries, string key)
        {
            var value = await ReadDatabaseConfigEntry(session, queries, key) ??
                throw new Exception($"Client version key '{key}' not specified in database configuration table");

            if (!uint.TryParse(value, out var result))
                throw new Exception($"Value '{value}' specified for client version key '{key}' is not a properly formed unsigned integer");

            return result;
        }

        async Task<AvatarConfig> ReadAvatarFromDatabase(ISession session, Queries queries)
        {
            var value = await ReadDatabaseConfigEntry(session, queries, "avatar") ??
                throw new Exception($"Avatar key 'avatar' not specified in database configuration table");

            return ParseJson<AvatarConfig>(value);
        }

        async Task<List<BotConfig>> ReadBotsFromDatabase(ISession session, Queries queries)
        {
            var value = await ReadDatabaseConfigEntry(session, queries, "bots") ??
                throw new Exception($"Bot config key 'bots' not specified in database configuration table");

            return ParseJson<List<BotConfig>>(value);
        }

        async Task InternalUpdateConfigFromDatabase()
        {
            var connectionString = systemSettingsProvider.Settings.Values.ConnectionString;
            var session = await CassandraSessionFactory.CreateSession(connectionString);
            var queries = await Queries.CreateInstance(session);

            var newData = await ReadConfigDataFromDatabase(session, queries);
            (newData.Groups, newData.Categories, newData.RenamedCategories, newData.LatestClientVersion, newData.LastCompatibleClientVersion, newData.AvatarConfig, newData.Bots) = 
                await ReadNonJsonConfigDataFromDatabase(session, queries);

            await SetNewData(newData, session, queries);
        }

        async Task<int> ReadAndIncrementDataVersion(ISession session, Queries queries)
        {
            const string key = "config-version";

            var value = await ReadDatabaseConfigEntry(session, queries, key);
            if (!int.TryParse(value, out var currentVersion))
            {
                logger.LogWarning("Failed to read config version from database, will assume 0");
                currentVersion = 0;
            }

            ++currentVersion;
            await WriteDatabaseConfigEntry(session, queries, key, currentVersion.ToString());

            return currentVersion;
        }

        async Task SetNewData(ConfigData newData, ISession session, Queries queries, bool validate = true)
        {
            if (validate)
                ReadOnlyConfigData.Validate(newData);
            newData.Version = await ReadAndIncrementDataVersion(session, queries);
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
            var queries = await Queries.CreateInstance(session);

            var data = GetData();
            var newData = ParseConfigData(jsonConfig);
            newData.Categories = data.Categories;
            newData.Groups = data.Groups;
            newData.RenamedCategories = data.RenamedCategories;
            newData.LatestClientVersion = data.LatestClientVersion;
            newData.LastCompatibleClientVersion = data.LastCompatibleClientVersion;
            newData.AvatarConfig = data.AvatarConfig;
            newData.Bots = data.Bots;

            ReadOnlyConfigData.Validate(newData);

            await WriteConfigToDatabase(jsonConfig, session, queries);

            await SetNewData(newData, session, queries, false);

            await PushUpdateToAllSilos();
        }

        static Task WriteConfigToDatabase(string jsonConfig, ISession session, Queries queries)
            => WriteDatabaseConfigEntry(session, queries, "config", jsonConfig);

        public async Task UploadAvatarConfig(string jsonConfig)
        {
            var connectionString = systemSettingsProvider.Settings.Values.ConnectionString;
            var session = await CassandraSessionFactory.CreateSession(connectionString);
            var queries = await Queries.CreateInstance(session);

            var data = GetData();
            var newData = (ConfigData)data.Clone();
            newData.AvatarConfig = ParseJson<AvatarConfig>(jsonConfig);

            ReadOnlyConfigData.Validate(newData);

            await WriteAvatarConfigToDatabase(jsonConfig, session, queries);

            await SetNewData(newData, session, queries, false);

            await PushUpdateToAllSilos();
        }

        static Task WriteAvatarConfigToDatabase(string jsonConfig, ISession session, Queries queries)
            => WriteDatabaseConfigEntry(session, queries, "avatar", jsonConfig);

        public async Task UploadBotsConfig(string jsonConfig)
        {
            var connectionString = systemSettingsProvider.Settings.Values.ConnectionString;
            var session = await CassandraSessionFactory.CreateSession(connectionString);
            var queries = await Queries.CreateInstance(session);

            var data = GetData();
            var newData = (ConfigData)data.Clone();
            newData.Bots = ParseJson<List<BotConfig>>(jsonConfig);

            ReadOnlyConfigData.Validate(newData);

            await WriteBotsConfigToDatabase(jsonConfig, session, queries);

            await SetNewData(newData, session, queries, false);

            await PushUpdateToAllSilos();
        }

        static Task WriteBotsConfigToDatabase(string jsonConfig, ISession session, Queries queries)
            => WriteDatabaseConfigEntry(session, queries, "bots", jsonConfig);
    }
}
