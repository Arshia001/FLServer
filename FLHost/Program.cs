using FLGrainInterfaces;
using FLGrains;
using LightMessage.Common.ProtocolMessages;
using LightMessage.OrleansUtils.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using OrleansBondUtils.CassandraInterop;
using OrleansCassandraUtils;
using OrleansCassandraUtils.Clustering;
using OrleansCassandraUtils.Persistence;
using OrleansCassandraUtils.Reminders;
using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace FLHost
{
    class Program
    {
        static IClusterClient client;
        static ISiloHost silo;
        static LightMessageHost lightMessageHost;

        static async Task Main(string[] args)
        {
            int retryCounter = 0;

            while (true)
            {
                try
                {
                    silo = new SiloHostBuilder()
                        .ConfigureServices(e => ServiceConfiguration.ConfigureServices(e, "Contact Point=localhost;KeySpace=fl_server_dev;Compression=Snappy"))
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
                        .ConfigureLogging(l => l.AddFilter("Orleans", LogLevel.Information).AddConsole())
                        .ConfigureApplicationParts(p =>
                        {
                            p.AddApplicationPart(typeof(TestGrain).Assembly).WithReferences();
                            p.AddApplicationPart(typeof(LightMessage.OrleansUtils.Grains.EndPointGrain).Assembly).WithReferences();
                            p.AddApplicationPart(typeof(OrleansIndexingGrains.IndexerGrainUnique<,>).Assembly).WithReferences();
                        })
                        .Configure<SerializationProviderOptions>(o =>
                        {
                            o.SerializationProviders.Add(typeof(LightMessage.OrleansUtils.GrainInterfaces.LightMessageSerializer).GetTypeInfo());
                        })
                        .Configure<ProcessExitHandlingOptions>(o => o.FastKillOnProcessExit = false)
                        .AddStartupTask<ConfigStartupTask>()
                        .Build();

                    await silo.StartAsync();

                    Console.CancelKeyPress += Console_CancelKeyPress;
                    AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

                    client = silo.Services.GetRequiredService<IClusterClient>();

                    lightMessageHost = new LightMessageHost();
                    await lightMessageHost.Start(client, new IPEndPoint(IPAddress.Any, 7510), OnAuth, new LightMessage.Common.Util.ConsoleLogProvider(LightMessage.Common.Util.LogLevel.Verbose));

                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    if (++retryCounter < 3)
                    {
                        await Task.Delay(10_000);
                        continue;
                    }
                    else
                    {
                        Environment.Exit(-1);
                        return;
                    }
                }
            }

            await silo.Stopped;
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("AppDomain.ProcessExit was raised");
            Shutdown().Wait();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Console.CancelKeyPress was raised");
            Shutdown().Wait();
        }

        static async Task Shutdown()
        {
            lightMessageHost.Stop();
            await silo.StopAsync();
        }

        static Task<Guid?> OnAuth(Guid? clientID)
        {
            return Task.FromResult(clientID ?? (Guid?)Guid.NewGuid()); //?? client.GetGrain<IClientAuthorizer>(0).Authorize(AuthMessage);
        }
    }
}
