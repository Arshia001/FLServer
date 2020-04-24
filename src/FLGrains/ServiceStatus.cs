using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrains.ServiceInterfaces;
using Orleans;
using System.Threading.Tasks;

namespace FLGrains
{
    class ServiceStatus : Grain, IServiceStatus
    {
        private readonly ISystemSettingsProvider systemSettings;
        private readonly IConfigReader configReader;

        public ServiceStatus(ISystemSettingsProvider systemSettings, IConfigReader configReader)
        {
            this.systemSettings = systemSettings;
            this.configReader = configReader;
        }

        public Task<(uint latest, uint minimumSupported, uint lastCompatible)> GetClientVersion()
        {
            var config = configReader.Config;
            return Task.FromResult((config.LatestClientVersion, systemSettings.Settings.Values.MinimumSupportedVersion, config.LastCompatibleClientVersion));
        }
    }
}
