using FLGrainInterfaces;
using FLGrains.ServiceInterfaces;
using Orleans;
using System.Threading.Tasks;

namespace FLGrains
{
    class ServiceStatus : Grain, IServiceStatus
    {
        private readonly ISystemSettingsProvider systemSettings;

        public ServiceStatus(ISystemSettingsProvider systemSettings) => this.systemSettings = systemSettings;

        public Task<(uint latest, uint minimumSupported)> GetClientVersion() =>
            Task.FromResult((systemSettings.Settings.Values.LatestVersion, systemSettings.Settings.Values.MinimumSupportedVersion));
    }
}
