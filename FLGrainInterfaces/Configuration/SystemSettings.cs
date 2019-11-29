using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FLGrainInterfaces.Configuration
{
    public class SystemSettings
    {
        class JsonValues
        {
            public JsonValues(string connectionString, uint latestVersion, uint minimumSupportedVersion)
            {
                ConnectionString = connectionString;
                LatestVersion = latestVersion;
                MinimumSupportedVersion = minimumSupportedVersion;
            }

            public string ConnectionString { get; }

            public uint LatestVersion { get; }
            public uint MinimumSupportedVersion { get; }
        }

        public SystemSettings(string jsonValues, string fcmServiceAccountKeys)
        {
            FcmServiceAccountKeys = fcmServiceAccountKeys;

            var values = JsonConvert.DeserializeObject<JsonValues>(jsonValues);
            ConnectionString = values.ConnectionString;
            LatestVersion = values.LatestVersion;
            MinimumSupportedVersion = values.MinimumSupportedVersion;
        }

        public string FcmServiceAccountKeys { get; }
        
        public string ConnectionString { get; }
        
        public uint LatestVersion { get; }
        public uint MinimumSupportedVersion { get; }
    }
}
