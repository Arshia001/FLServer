namespace FLServiceStatus

open FSharp.Control.Tasks.V2
open FSharp.Data
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Orleans
open Orleans.Configuration
open OrleansCassandraUtils

type SystemSettings = JsonProvider<"system-settings.json">

module Program =
    let buildClient () =
        let settings = SystemSettings.Parse(File.ReadAllText("system-settings.json"))

        let client =
            ClientBuilder()
                .UseCassandraClustering(fun (o: Clustering.CassandraClusteringOptions) -> 
                    o.ConnectionString <- settings.ConnectionString
                )
                .ConfigureLogging(fun l -> 
                    l.AddFilter("Orleans", LogLevel.Information).AddConsole() |> ignore
                )
                .Configure<ClusterOptions>(fun (c: ClusterOptions) ->
                    c.ClusterId <- "FLCluster"
                    c.ServiceId <- "FLService"
                )
                .Build()

        client

    let createHostBuilder (args, client: IClusterClient) =
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webBuilder ->
                webBuilder.UseStartup<Startup>() |> ignore
            )
            .ConfigureServices(fun s ->
                s
                    .AddSingleton<IClusterClient>(client)
                    .AddSingleton<IStatusMonitorService, StatusMonitorService>()
                    .AddHostedService<StatusMonitorHostedService>()
                    |> ignore
            )

    [<EntryPoint>]
    let main args = 
        (task {
            let clusterClient = buildClient()
            use host = createHostBuilder(args, clusterClient).Build()

            do! host.StartAsync()

            let mutable exitRequested = false

            host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register (fun () -> 
                exitRequested <- true
                ) |> ignore

            do! clusterClient.Connect(fun exn -> task {
                printfn "Failed to connect to any silos, will retry. Exception: %A" exn
                do! Task.Delay(TimeSpan.FromSeconds(2.))
                return not exitRequested
            })

            do! host.WaitForShutdownAsync()

            return 0
        }) |> Async.AwaitTask |> Async.RunSynchronously
