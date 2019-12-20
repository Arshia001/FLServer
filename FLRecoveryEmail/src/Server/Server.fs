open System.IO

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection

open Giraffe
open Saturn

open Fable.Remoting.Server
open Fable.Remoting.Giraffe

open Shared
open SettingsProvider
open ClusterClientProvider

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

let app = application {
    url ("http://0.0.0.0:" + settings.Port.ToString() + "/")
    use_router (choose [webApp])
    memory_cache
    use_static publicPath
    use_gzip
    service_config (fun s ->
        s
            .AddSingleton<ISettingsProvider>(SettingsProvider(settings))
            .AddSingleton<IClusterClientProvider, ClusterClientProvider>()
        )
}

run app
