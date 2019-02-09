using FLGameLogic;
using FLGrainInterfaces;
using FLGrains;
using LightMessage.Client;
using LightMessage.Client.EndPoints;
using LightMessage.Common.Messages;
using LightMessage.Common.ProtocolMessages;
using LightMessage.Common.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using OrleansCassandraUtils;
using OrleansCassandraUtils.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FLTestClient
{
    class Program
    {
        static Guid ID(int i) => new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        static void Main(string[] args)
        {
            //var svc = new ServiceCollection();
            //ServiceConfiguration.ConfigureServices(svc, "Contact Point=localhost;KeySpace=fl_server_dev;Compression=Snappy");
            //var provider = svc.BuildServiceProvider();

            //var sgs = provider.GetRequiredService<ISuggestionService>();

            //var all = sgs.GetAllCategorySuggestions().Result;
            //var bu = sgs.GetCategorySuggestionsByUser(Guid.Parse("9a6b82de-1176-4a33-8a46-3dc526be7716")).Result;

            //all = sgs.GetAllWordSuggestions().Result;
            //bu = sgs.GetWordSuggestionsByUser(Guid.Parse("1422325f-cefe-4594-9198-b34c7faa5123")).Result;



            //var s = CassandraSessionFactory.CreateSession("Contact Point=localhost;KeySpace=fl_server_dev;Compression=Snappy").Result;
            //var q = Queries.CreateInstance(s).Result;
            //s.Execute(q["fl_UpsertSuggestedCategory"].Bind(new
            //{
            //    name = "naaaammmeee",
            //    owner_id = Guid.NewGuid(),
            //    words = new HashSet<string> { "a", "b", "c" }.Select(xxx => xxx)
            //}));
            //s.Execute(q["fl_UpsertSuggestedWords"].Bind(new
            //{
            //    category_name = "naaaammmeee",
            //    owner_id = Guid.NewGuid(),
            //    words = new HashSet<string> { "a", "b", "c" }
            //}));

            //var x = Utility.EditDistanceLessThan("گیلاس", "گلابی", 3);
            //x = Utility.EditDistanceLessThan("گلابی", "گیلاس", 2);
            //x = Utility.EditDistanceLessThan("hello", "hallo", 3);
            //x = Utility.EditDistanceLessThan("hello", "he", 3);
            //x = Utility.EditDistanceLessThan("hello", "lo", 3);
            //x = Utility.EditDistanceLessThan("lo", "hallo", 3);
            //x = Utility.EditDistanceLessThan("hellooooooo", "hallo", 3);
            //x = Utility.EditDistanceLessThan("helloooo", "hello", 3);
            //x = Utility.EditDistanceLessThan("heallo", "hillo", 3);



            var client = new ClientBuilder()
                .Configure<ClusterOptions>(o =>
                {
                    o.ClusterId = "FLCluster";
                    o.ServiceId = "FLService";
                })
                .ConfigureApplicationParts(p => p.AddApplicationPart(typeof(IGame).Assembly))
                .UseCassandraClustering(o => o.ConnectionString = "Contact Point=localhost;KeySpace=fl_server_dev;Compression=Snappy")
                .ConfigureLogging(l => l.AddFilter("Orleans", Microsoft.Extensions.Logging.LogLevel.Information).AddConsole())
                .Build();
            client.Connect().Wait();

            var worker = client.GetGrain<IWordUsageAggregationWorker>("xxx");
            Task.WhenAll(
                worker.AddDelta("hello1"),
                worker.AddDelta("hello1"),
                worker.AddDelta("hello1"),
                worker.AddDelta("hello1"),
                worker.AddDelta("hello1"),
                worker.AddDelta("hello1"),
                worker.AddDelta("hello2"),
                worker.AddDelta("hello2"),
                worker.AddDelta("hello2")
                ).Wait();

            var ret = client.GetGrain<IWordUsageAggregatorCache>("xxx");
            foreach (var i in Enumerable.Range(0, 10))
            {
                Task.Delay(5000).Wait();

                Task.WhenAll(
                    worker.AddDelta("hello1"),
                    worker.AddDelta("hello1"),
                    worker.AddDelta("hello1"),
                    worker.AddDelta("hello1"),
                    worker.AddDelta("hello1"),
                    worker.AddDelta("hello1"),
                    worker.AddDelta("hello2"),
                    worker.AddDelta("hello2"),
                    worker.AddDelta("hello2")
                    ).Wait();

                Console.WriteLine(string.Join("; ", ret.GetData().Result.Select(kv => $"({kv.Key}, {kv.Value})")));
            }


            //var client = new EndPointClient(new ConsoleLogProvider(LightMessage.Common.Util.LogLevel.Verbose));
            //var endpoint = client.CreateProxy("game");
            //client.Connect(new IPEndPoint(IPAddress.Loopback, 1020), CancellationToken.None, new AuthRequestMessage(Param.Guid(Guid.NewGuid())), true).Wait();

            //Guid gameID = Guid.NewGuid();
            //while (true)
            //{
            //    try
            //    {
            //        var parts = Console.ReadLine().Split(' ');

            //        switch (parts[0])
            //        {
            //            case "e":
            //            case "exit":
            //                return;

            //            case "n":
            //            case "j":
            //            case "j1":
            //                {
            //                    //client.GetGrain<IGame>(gameID).StartNew(ID(int.Parse(parts[1]))).Wait();
            //                    var res = endpoint.SendInvocationForReply("new", CancellationToken.None).Result;
            //                    gameID = res[0].AsGuid.Value;
            //                    Console.WriteLine("Opponent name: " + res[1].AsString ?? "Unknown yet");
            //                }
            //                break;

            //            case "j2":
            //                {
            //                    // client.GetGrain<IGame>(gameID).AddSecondPlayer(ID(int.Parse(parts[1]))).Wait();
            //                    var res = endpoint.SendInvocationForReply("new", CancellationToken.None).Result;
            //                    gameID = res[0].AsGuid.Value;
            //                    Console.WriteLine("Opponent name: " + res[1].AsString ?? "Unknown yet");
            //                }
            //                break;

            //            case "t":
            //            case "r":
            //                {
            //                    // client.GetGrain<IGame>(gameID).StartRound(ID(int.Parse(parts[1]))).Wait();
            //                    var res = endpoint.SendInvocationForReply("round", CancellationToken.None, Param.Guid(gameID)).Result;
            //                    Console.WriteLine($"Category: {res[0].AsString}, turn time: {res[1].AsTimeSpan}");
            //                }
            //                break;

            //            //case "p":
            //            //case "w":
            //            //    Console.WriteLine(client.GetGrain<IGame>(gameID).PlayWord(ID(int.Parse(parts[1])), parts[2]).Result);
            //            //    break;

            //            default:
            //                {
            //                    var res = endpoint.SendInvocationForReply("word", CancellationToken.None, Param.Guid(gameID), Param.String(parts[0])).Result;
            //                    Console.WriteLine($"Total score: {res[0].AsUInt}, this word: {(res[1].AsInt.Value >= 0 ? res[1].AsInt.Value.ToString() : res[2].IsNull ? "Duplicate" : "Duplicate of " + res[2].AsString)}");
            //                }
            //                break;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex);
            //    }
            //}
        }
    }
}
