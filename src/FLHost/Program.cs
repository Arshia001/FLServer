using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrains;
using FLGrains.Configuration;
using FLGrains.ServiceInterfaces;
using LightMessage.Common.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FLHost
{
    class Program
    {
        static IClusterClient? client;

        static async Task Main(string[] args)
        {
            try
            {
                var systemSettings = new SystemSettings(File.ReadAllText("system-settings.json"), File.ReadAllText("firebase-adminsdk-accountkeys.json"));

                var host = new HostBuilder()
                    .ConfigureServices(e =>
                    {
                        e
                        .ConfigureGameServer(systemSettings)
                        .Configure<LightMessageOptions>(o =>
                        {
                            o.ListenIPAddress = IPAddress.Any;
                            o.ListenPort = 7510;
                            o.ClientAuthCallback = OnAuth;
                            o.ClientDisconnectedCallback = OnDisconnect;
                        })
                        .AddSingleton<ILogProvider>(new ConsoleLogProvider(LightMessage.Common.Util.LogLevel.Info))
                        .AddSingleton<LightMessageHostedService>()
                        .AddSingleton<ILightMessageHostAccessor>(sp => sp.GetRequiredService<LightMessageHostedService>())
                        .AddHostedService(sp => sp.GetRequiredService<LightMessageHostedService>())
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
                            o.ClassSpecificCollectionAge["FLGrains.Game"] = TimeSpan.FromMinutes(10);
                            o.ClassSpecificCollectionAge["FLGrains.Player"] = TimeSpan.FromMinutes(5);
                            o.ClassSpecificCollectionAge["FLGrains.LeaderBoard"] = TimeSpan.FromHours(24);
                            o.CollectionQuantum = TimeSpan.FromMinutes(3);
                        })
                        .UseCassandraClustering((CassandraClusteringOptions o) =>
                        {
                            o.ConnectionString = systemSettings.Values.ConnectionString;
                        })
                        .UseCassandraReminderService((CassandraReminderTableOptions o) =>
                        {
                            o.ConnectionString = systemSettings.Values.ConnectionString;
                        })
                        .AddCassandraGrainStorageAsDefault((CassandraGrainStorageOptions o) =>
                        {
                            o.ConnctionString = systemSettings.Values.ConnectionString;
                            o.AddSerializationProvider(1, new BondCassandraStorageSerializationProvider());
                        })
                        .ConfigureApplicationParts(p =>
                        {
                            p.AddApplicationPart(typeof(TestGrain).Assembly).WithReferences();
                            p.AddApplicationPart(typeof(LightMessage.OrleansUtils.Grains.EndPointGrain).Assembly).WithReferences();
                            p.AddApplicationPart(typeof(OrleansIndexingGrains.IndexerGrainUnique<,>).Assembly).WithReferences();
                        })
                        .AddStartupTask<ConfigStartupTask>()
                        ;

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            s.UsePerfCounterEnvironmentStatistics();
                        else
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
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start server due to {ex}, will exit in 5 seconds");
                await Task.Delay(5000);
                Environment.Exit(-1);
            }
        }

        static Task<Guid?> OnAuth(HandShakeMode mode, Guid? clientID, string? email, string? password) =>
            client?.GetGrain<IClientAuthenticator>(0).Authenticate(mode, clientID, email, password) ?? Task.FromResult(default(Guid?));

        static Task OnDisconnect(Guid clientID) =>
            client?.GetGrain<IPlayer>(clientID).PlayerDisconnected() ?? Task.CompletedTask;
    }
}
