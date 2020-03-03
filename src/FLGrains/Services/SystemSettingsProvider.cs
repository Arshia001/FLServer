using FLGrainInterfaces.Configuration;
using FLGrains.ServiceInterfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrains.Services
{
    class SystemSettingsProvider : ISystemSettingsProvider
    {
        public SystemSettings Settings { get; }

        public SystemSettingsProvider(SystemSettings settings) => Settings = settings;
    }
}
