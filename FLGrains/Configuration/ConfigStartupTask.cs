using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using Orleans;
using Orleans.Runtime;
using System.Threading;
using System.Threading.Tasks;

namespace FLGrains.Configuration
{
    public class ConfigStartupTask : IStartupTask
    {
        readonly IGrainFactory grainFactory;
        readonly IConfigWriter configWriter;

        public ConfigStartupTask(IGrainFactory grainFactory, IConfigWriter configWriter)
        {
            this.grainFactory = grainFactory;
            this.configWriter = configWriter;
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            configWriter.Config = (await grainFactory.GetGrain<ISystemConfig>(0).GetConfig()).Value;
        }
    }
}
