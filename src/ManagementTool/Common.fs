[<AutoOpen>]
module Common

open Microsoft.Extensions.Logging
open Orleans
open System.Threading.Tasks

let runSynchronously (t: Task) = t |> Async.AwaitTask |> Async.RunSynchronously

let buildOrleansClient () =
    let client =
        ClientBuilder()
            .UseLocalhostClustering(40000, "FLService", "FLCluster")
            .ConfigureLogging(fun l -> 
                l.AddFilter("Orleans", LogLevel.Information).AddConsole() |> ignore
            )
            .Build()

    client.Connect (fun _ -> Task.FromResult false) |> runSynchronously
    client
