using FLGrainInterfaces;
using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    class ServiceStatus : Grain, IServiceStatus
    {
        public Task<bool> GetStatus() => FLTaskExtensions.True;
    }
}
