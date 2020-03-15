using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FLGrains.ServiceInterfaces;
using Orleans.Providers;

namespace FLGrains.ServerStatistics
{
    class ServerStatisticsControllable : IControllable
    {
        public static string ServiceName = "ServerStatistics";

        private readonly ILightMessageHostAccessor lightMessageHostAccessor;

        public ServerStatisticsControllable(ILightMessageHostAccessor lightMessageHostAccessor)
        {
            this.lightMessageHostAccessor = lightMessageHostAccessor;
        }

        public Task<object> ExecuteCommand(int command, object arg)
        {
            switch ((Command)command)
            {
                case Command.GetConnectedClientCount:
                    return Task.FromResult(lightMessageHostAccessor.Host.ConnectedClientCount as object);

                default:
                    throw new Exception($"Unknown command {command}");
            }
        }

        public enum Command
        {
            GetConnectedClientCount = 0
        }
    }
}
