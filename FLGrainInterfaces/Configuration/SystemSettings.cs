using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FLGrainInterfaces.Configuration
{
    public class SystemSettings
    {
        public class JsonValues
        {
            public JsonValues(string connectionString, uint latestVersion, uint minimumSupportedVersion, TimeSpan passwordRecoveryTokenExpirationInterval)
            {
                ConnectionString = connectionString;
                LatestVersion = latestVersion;
                MinimumSupportedVersion = minimumSupportedVersion;
                PasswordRecoveryTokenExpirationInterval = passwordRecoveryTokenExpirationInterval;
            }

            public string ConnectionString { get; }

            public uint LatestVersion { get; }
            public uint MinimumSupportedVersion { get; }

            public TimeSpan PasswordRecoveryTokenExpirationInterval { get; }
        }

        public SystemSettings(string jsonValues, string fcmServiceAccountKeys)
        {
            FcmServiceAccountKeys = fcmServiceAccountKeys;
            Values = JsonConvert.DeserializeObject<JsonValues>(jsonValues);
        }

        public string FcmServiceAccountKeys { get; }

        public JsonValues Values { get; }
    }
}
