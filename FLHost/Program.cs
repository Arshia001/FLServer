using FLGrainInterfaces;
using FLGrains;
using FLGrains.Configuration;
using LightMessage.Common.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Statistics;
using OrleansBondUtils.CassandraInterop;
using OrleansCassandraUtils;
using OrleansCassandraUtils.Clustering;
using OrleansCassandraUtils.Persistence;
using OrleansCassandraUtils.Reminders;
using System;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FLHost
{
    class Program
    {
        static IClusterClient client;

        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureServices(e =>
                {
                    e
                        .ConfigureGameServer("Contact Point=localhost;KeySpace=fl_server_dev;Compression=Snappy")
                        .Configure<ProcessExitHandlingOptions>(o => o.FastKillOnProcessExit = false)
                        .Configure<LightMessageOptions>(o =>
                        {
                            o.ListenIPAddress = IPAddress.Any;
                            o.ListenPort = 7510;
                            o.ClientAuthCallback = OnAuth;
                        })
                        .AddSingleton<ILogProvider>(new ConsoleLogProvider(LightMessage.Common.Util.LogLevel.Verbose))
                        .AddHostedService<LightMessageHostedService>();
                    ;
                })
                .ConfigureLogging(l => l.AddFilter("Orleans", Microsoft.Extensions.Logging.LogLevel.Information).AddConsole())
                .UseOrleans(s =>
                {
                    s
                        .Configure<ClusterOptions>(o =>
                        {
                            o.ClusterId = "FLCluster";
                            o.ServiceId = "FLService";
                        })
                        .Configure<EndpointOptions>(o =>
                        {
                            o.AdvertisedIPAddress = IPAddress.Parse("127.0.0.1");
                            o.GatewayPort = 40000;
                            o.SiloPort = 11111;
                        })
                        .Configure<SchedulingOptions>(o => o.AllowCallChainReentrancy = true)
                        .Configure<SerializationProviderOptions>(o =>
                        {
                            o.SerializationProviders.Add(typeof(LightMessage.OrleansUtils.GrainInterfaces.LightMessageSerializer).GetTypeInfo());
                        })
                        .Configure<LoadSheddingOptions>(o => o.LoadSheddingEnabled = true)
                        .Configure<GrainCollectionOptions>(o =>
                        {
                            o.ClassSpecificCollectionAge["FLGrains.Game"] = TimeSpan.FromMinutes(5);
                            o.CollectionQuantum = TimeSpan.FromMinutes(2);
                        })
                        .UseCassandraClustering((CassandraClusteringOptions o) =>
                        {
                            o.ConnectionString = "Contact Point=localhost;KeySpace=fl_server_dev;Compression=Snappy";
                        })
                        .UseCassandraReminderService((CassandraReminderTableOptions o) =>
                        {
                            o.ConnectionString = "Contact Point=localhost;KeySpace=fl_server_dev;Compression=Snappy";
                        })
                        .AddCassandraGrainStorageAsDefault((CassandraGrainStorageOptions o) =>
                        {
                            o.ConnctionString = "Contact Point=localhost;KeySpace=fl_server_dev;Compression=Snappy";
                            o.AddSerializationProvider(1, new BondCassandraStorageSerializationProvider());
                        })
                        .ConfigureApplicationParts(p =>
                        {
                            p.AddApplicationPart(typeof(TestGrain).Assembly).WithReferences();
                            p.AddApplicationPart(typeof(LightMessage.OrleansUtils.Grains.EndPointGrain).Assembly).WithReferences();
                            p.AddApplicationPart(typeof(OrleansIndexingGrains.IndexerGrainUnique<,>).Assembly).WithReferences();
                        })
                        .AddStartupTask<ConfigStartupTask>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                s.UsePerfCounterEnvironmentStatistics();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                s.UseLinuxEnvironmentStatistics();
        })
                .Build();

            using (host)
            {
                await host.StartAsync();

    client = host.Services.GetRequiredService<IClusterClient>();

                await host.WaitForShutdownAsync();
}
        }

        static Task<Guid?> OnAuth(HandShakeMode mode, Guid? clientID, string email, string password) =>
            client.GetGrain<IClientAuthenticator>(0).Authenticate(mode, clientID, email, password);
    }
}
