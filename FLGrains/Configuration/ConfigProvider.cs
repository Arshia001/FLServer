using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using System;

namespace FLGrains.Configuration
{
    class ConfigProvider : IConfigReader, IConfigWriter
    {
        volatile ReadOnlyConfigData? ConfigData;
        public int Version => ConfigData?.Version ?? int.MinValue;

        ReadOnlyConfigData IConfigReader.Config => ConfigData ?? throw new Exception("Config data not initialized yet");

        ConfigData IConfigWriter.Config
        {
            set
            {
                if (value != null)
                    ConfigData = new ReadOnlyConfigData(value);
            }
        }
    }
}
