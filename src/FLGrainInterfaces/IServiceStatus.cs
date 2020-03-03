using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public interface IServiceStatus : IGrainWithIntegerKey
    {
        Task<(uint latest, uint minimumSupported)> GetClientVersion();
    }
}
