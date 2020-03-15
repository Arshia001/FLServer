using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FLGrainInterfaces.ServerStatistics;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace FLGrains.ServerStatistics
{
    [StatelessWorker]
    class ServerStatistics : Grain, IServerStatistics
    {
        public async Task<int> GetConnectedClientCount()
        {
            var counts = await GrainFactory.GetGrain<IManagementGrain>(0).SendControlCommandToProvider(
                typeof(ServerStatisticsControllable).FullName, 
                ServerStatisticsControllable.ServiceName, 
                (int)ServerStatisticsControllable.Command.GetConnectedClientCount
            );
            
            return counts.Select(c => (int)c).Sum();
        }
    }
}
