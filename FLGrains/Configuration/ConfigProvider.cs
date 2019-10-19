using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;

namespace FLGrains.Configuration
{
    class ConfigProvider : IConfigReader, IConfigWriter
    {
        volatile ReadOnlyConfigData ConfigData;
        public int Version => ConfigData?.Version ?? int.MinValue;

        ReadOnlyConfigData IConfigReader.Config => ConfigData;

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
