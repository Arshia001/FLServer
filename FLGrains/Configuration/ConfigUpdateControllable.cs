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

        IGrainFactory GrainFactory;
        IConfigWriter ConfigWriter;

        public ConfigUpdateControllable(IGrainFactory GrainFactory, IConfigWriter ConfigWriter)
        {
            this.GrainFactory = GrainFactory;
            this.ConfigWriter = ConfigWriter;
        }

        public async Task<object?> ExecuteCommand(int command, object arg)
        {
            if (command > ConfigWriter.Version)
                ConfigWriter.Config = (await GrainFactory.GetGrain<ISystemConfig>(0).GetConfig()).Value;
            return null;
        }
    }
}
