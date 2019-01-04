using FLGrainInterfaces;
using FLGrains;
using LightMessage.Common.ProtocolMessages;
using LightMessage.OrleansUtils.Host;
using Microsoft.Extensions.DependencyInjection;
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
        static async Task Main(string[] args)
        {
            var silo = new SiloHostBuilder()
                .ConfigureServices(e => e.AddSingleton<IGrainReferenceConversionProvider, BondGrainReferenceConversionProvider>())
                .Configure<ClusterOptions>(o =>
                {
                    o.ClusterId = "FLCluster";
                    o.ServiceId = "FLService";
                })
                .Configure<EndpointOptions>(o =>
                {
                    o.AdvertisedIPAddress = IPAddress.Parse("127.0.0.1");
                    o.GatewayPort = 40001;
                    o.SiloPort = 11112;
                })
                .Configure<SchedulingOptions>(o => o.AllowCallChainReentrancy = true)
                .EnableDirectClient()
                .UseCassandraClustering((CassandraClusteringOptions o) =>
                {
                    o.ConnectionString = "Contact Point=localhost;KeySpace=fl_server_dev;Compression=Snappy";
                })
                .UseCassandraReminderService((CassandraReminderTableOptions o) =>
                {
                    o.ConnectionString = "Contact Point=localhost;KeySpace=fl_server_dev;Compression=Snappy";
                })
                .AddMemoryGrainStorageAsDefault()
                //.AddCassandraGrainStorageAsDefault((CassandraGrainStorageOptions o) =>
                //{
                //    o.ConnctionString = "Contact Point=localhost;KeySpace=fl_server_dev;Compression=Snappy";
                //    o.AddSerializationProvider(1, new BondCassandraStorageSerializationProvider());
                //})
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
                .Build();

            await silo.StartAsync();

            var client = (IClusterClient)silo.Services.GetService(typeof(IClusterClient));

            var lightMessageHost = new LightMessageOrleansHost();
            await lightMessageHost.Start(client, new IPEndPoint(IPAddress.Any, 1021), OnAuth, new LightMessage.Common.Util.ConsoleLogProvider(LightMessage.Common.Util.LogLevel.Verbose));

            while (Console.ReadLine() != "exit")
                Console.WriteLine("Type 'exit' to stop silo and exit");

            await silo.StopAsync();
        }

        static Task<Guid?> OnAuth(AuthRequestMessage AuthMessage)
        {
            return Task.FromResult(AuthMessage.AsGuid(0) ?? (Guid?)Guid.NewGuid()); //?? client.GetGrain<IClientAuthorizer>(0).Authorize(AuthMessage);
        }
    }
}
