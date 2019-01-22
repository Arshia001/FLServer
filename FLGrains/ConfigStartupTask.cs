using FLGrainInterfaces;
using Orleans;
using Orleans.Runtime;
using System.Threading;
using System.Threading.Tasks;

namespace FLGrains
{
    public class ConfigStartupTask : IStartupTask
    {
        IGrainFactory GrainFactory;
        IConfigWriter ConfigWriter;

        public ConfigStartupTask(IGrainFactory GrainFactory, IConfigWriter ConfigWriter)
        {
            this.GrainFactory = GrainFactory;
            this.ConfigWriter = ConfigWriter;
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            ConfigWriter.Config = (await GrainFactory.GetGrain<ISystemConfig>(0).GetConfig()).Value;
        }
    }
}
