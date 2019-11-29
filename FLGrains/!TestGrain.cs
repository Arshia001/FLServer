using FLGrainInterfaces;
using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    public class TestGrainState
    {
        public string SomeProperty1 { get; set; } = "";
    }

    public class TestGrain : Grain<TestGrainState>, ITestGrain
    {
        public async Task<string> SayHello(string name)
        {
            State.SomeProperty1 = name;

            await ClearStateAsync();

            return $"Hello, {name}!";
        }
    }
}
