using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public interface ITestGrain : IGrainWithIntegerKey
    {
        Task<string> SayHello(string name);
    }
}
