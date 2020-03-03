using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using Orleans;
using Orleans.Providers;
using System.Threading.Tasks;

namespace FLGrains.Configuration
{
    public class ConfigUpdateControllable : IControllable
    {
        public static string ServiceName = "ConfigUpdater";

        readonly IGrainFactory grainFactory;
        readonly IConfigWriter configWriter;

        public ConfigUpdateControllable(IGrainFactory grainFactory, IConfigWriter configWriter)
        {
            this.grainFactory = grainFactory;
            this.configWriter = configWriter;
        }

        public async Task<object?> ExecuteCommand(int command, object arg)
        {
            if (command > configWriter.Version)
                configWriter.Config = (await grainFactory.GetGrain<ISystemConfig>(0).GetConfig()).Value;
            return null;
        }
    }
}
