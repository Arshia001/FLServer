using FLGrains;
using FLGrains.ServiceInterfaces;
using LightMessage.Common.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FLHost
{
    //!! Move this into LightMessage itself? There's the issue of the compiler generating a host class for each project...
    class LightMessageHostedService : IHostedService, ILightMessageHostAccessor
    {
        private readonly IGrainFactory grainFactory;
        readonly IOptions<LightMessageOptions> options;
        readonly ILogProvider logProvider;

        LightMessageHost? host;

        public LightMessageHost Host => host ?? throw new Exception("Premature usage of LightMessageHostedService.Host");

        public LightMessageHostedService(IGrainFactory grainFactory, IOptions<LightMessageOptions> options, ILogProvider logProvider)
        {
            this.grainFactory = grainFactory;
            this.options = options;
            this.logProvider = logProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            host = new LightMessageHost();
            return host.Start(grainFactory, new IPEndPoint(options.Value.ListenIPAddress!, options.Value.ListenPort),
                options.Value.ClientAuthCallback ?? throw new Exception("Client authentication callback not set"),
                options.Value.ClientDisconnectedCallback ?? throw new Exception("Client disconnect callback not set"),
                logProvider);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            host?.Stop();
            return Task.CompletedTask;
        }
    }
}
