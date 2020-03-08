open System
open System.IO
open System.Threading.Tasks

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection

open Giraffe
open Saturn

open Fable.Remoting.Server
open Fable.Remoting.Giraffe

open Shared
open SettingsProvider
open ClusterClientProvider

open Orleans
open Orleans.Configuration
open OrleansCassandraUtils

open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2

let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"

let settings = SystemSettings.Parse(File.ReadAllText("system-settings.json"))

let recoveryEmailApi = reader {
    let! clusterClientProvider = resolve<IClusterClientProvider>()
    return RecoveryEmailApi.createApi clusterClientProvider
}

let webApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromReader recoveryEmailApi
    |> Remoting.buildHttpHandler

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

    let mutable retries = 0

    client.Connect(fun exn -> task {
        if retries < settings.MaxRetries then
            retries <- retries + 1
            printfn "Failed to connect to any silos, will retry. Retry count: %i; Exception: %A" retries exn
            do! Task.Delay(TimeSpan.FromSeconds(2.))
            return true
        else
            printfn "Failed to connect to any silos after %i retries, will give up. Exception: %A" retries exn
            return false
    }).Wait()

    client

try
    let clusterClient = buildClient(settings)

    let app = application {
        url ("http://127.0.0.1:" + settings.Port.ToString() + "/")
        use_router (choose [webApp])
        memory_cache
        use_static publicPath
        use_gzip
        service_config (fun s ->
            s
                .AddSingleton<ISettingsProvider>(SettingsProvider(settings))
                .AddSingleton<IClusterClientProvider>(ClusterClientProvider(clusterClient))
            )
    }

    run app
with exn ->
    printfn "Failed to start, will shut down in 5 seconds: %A" exn
    System.Threading.Thread.Sleep 5000
    Environment.Exit -1
