using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    class SaveStateOnDeactivateGrain<TState> : Grain<TState>
        where TState : new()
    {
        bool isStateCleared = false;


        public override Task OnDeactivateAsync()
        {
            if (isStateCleared)
                return base.OnDeactivateAsync();

            return WriteStateAsync().ContinueWith(t => base.OnDeactivateAsync()).Unwrap();
        }

        protected override Task ClearStateAsync()
        {
            isStateCleared = true;
            return base.ClearStateAsync();
        }
    }
}
