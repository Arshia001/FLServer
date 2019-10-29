using FLGrains.ServiceInterfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrains.Services
{
    class SystemSettingsProvider : ISystemSettingsProvider
    {
        public string ConnectionString { get; }

        public string FcmServiceAccountKeys { get; }

        public SystemSettingsProvider(string connectionString, string fcmServiceAccountKeys)
        {
            ConnectionString = connectionString;
            FcmServiceAccountKeys = fcmServiceAccountKeys;
        }
    }
}
