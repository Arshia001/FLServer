module ClusterClientProvider

open System
open System.Threading.Tasks

open Orleans
open Orleans.Configuration
open OrleansCassandraUtils

open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting
open FSharp.Control.Tasks.V2

open SettingsProvider

type IClusterClientProvider =
    abstract ClusterClient : IClusterClient with get

type ClusterClientProvider(settingsProvider: ISettingsProvider, lifetime: IHostApplicationLifetime) =
    let mutable exitRequested = false

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
    
        client.Connect(fun exn -> task {
            printfn "Failed to connect to any silos, will retry. Exception: %A" exn
            do! Task.Delay(TimeSpan.FromSeconds(2.))
            return not exitRequested
        }).Wait()
    
        client
    
    do lifetime.ApplicationStopping.Register(fun () -> exitRequested <- true) |> ignore

    let client = buildClient settingsProvider.Settings

    interface IClusterClientProvider with
        member _.ClusterClient = client
