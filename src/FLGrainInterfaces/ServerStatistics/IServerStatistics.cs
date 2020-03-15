using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans;

namespace FLGrainInterfaces.ServerStatistics
{
    public interface IServerStatistics : IGrainWithIntegerKey
    {
        Task<int> GetConnectedClientCount();
    }
}
