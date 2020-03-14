namespace FLServiceStatus

open FSharp.Control.Tasks.V2
open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Orleans
open Orleans.Configuration
open OrleansCassandraUtils

module Program =
    let buildClient (settings: SystemSettings.Root) =
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

    let createHostBuilder (args, client: IClusterClient, settings: SystemSettings.Root) =
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webBuilder ->
                webBuilder
                    .UseStartup<Startup>()
                    .UseUrls(sprintf "http://127.0.0.1:%i" settings.Port)
                    |> ignore
            )
            .ConfigureServices(fun s ->
                s
                    .AddSingleton<ISystemSettingsAccessor>(SystemSettingsAccessor(settings))
                    .AddSingleton<IClusterClient>(client)
                    .AddSingleton<IStatusMonitorService, StatusMonitorService>()
                    .AddHostedService<StatusMonitorHostedService>()
                    |> ignore
            )

    [<EntryPoint>]
    let main args = 
        (task {
            try
                let settings = SystemSettings.Parse(File.ReadAllText("system-settings.json"))

                let clusterClient = buildClient settings
                use host = createHostBuilder(args, clusterClient, settings).Build()

                do! host.StartAsync()

                let mutable exitRequested = false

                host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register (fun () -> 
                    exitRequested <- true
                    ) |> ignore

                let mutable retries = 0

                do! clusterClient.Connect(fun exn -> task {
                    if retries < settings.MaxRetries then
                        retries <- retries + 1
                        printfn "Failed to connect to any silos, will retry. Retry count: %i; Exception: %A" retries exn
                        do! Task.Delay(TimeSpan.FromSeconds(2.))
                        return not exitRequested
                    else
                        printfn "Failed to connect to any silos after %i retries, will give up. Exception: %A" retries exn
                        return false
                })

                do! host.WaitForShutdownAsync()

                return 0
            with exn ->
                printfn "Failed to start, will shut down in 5 seconds: %A" exn
                System.Threading.Thread.Sleep 5000
                return -1
        }) |> Async.AwaitTask |> Async.RunSynchronously
