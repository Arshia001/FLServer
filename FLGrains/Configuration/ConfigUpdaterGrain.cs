﻿using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using Orleans;
using Orleans.Placement;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;

namespace FLGrains.Configuration
{
    [PreferLocalPlacement]
    class ConfigUpdaterGrain : Grain, IConfigUpdaterGrain
    {
        IDisposable Timer;

        public Task PushUpdateToAllSilos(int Version)
        {
            if (Timer != null)
                Timer.Dispose();
            Timer = RegisterTimer(OnPushConfigTimer, Version, TimeSpan.FromMilliseconds(1), TimeSpan.MaxValue);
            return Task.CompletedTask;
        }

        async Task OnPushConfigTimer(object State)
        {
            try
            {
                Timer.Dispose();
                Timer = null;

                await GrainFactory.GetGrain<IManagementGrain>(0).SendControlCommandToProvider(typeof(ConfigUpdateControllable).FullName, ConfigUpdateControllable.ServiceName, (int)State);
            }
            catch
            {
                Timer = RegisterTimer(OnPushConfigTimer, State, TimeSpan.FromSeconds(10), TimeSpan.MaxValue);
            }
        }
    }
}
