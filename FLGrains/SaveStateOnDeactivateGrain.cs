using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    class SaveStateOnDeactivateGrain<TState> : Grain<TState>
        where TState: new()
    {
        public override Task OnDeactivateAsync()
        {
            return WriteStateAsync();
        }
    }
}
