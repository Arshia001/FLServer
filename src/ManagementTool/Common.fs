[<AutoOpen>]
module Common

open System
open Microsoft.Extensions.Logging
open Orleans
open System.Threading.Tasks
open OrleansCassandraUtils.Utils

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

let buildCassandraSession (keyspace: string) =
    let session = CassandraSessionFactory.CreateSession(sprintf "Contact Point=localhost;KeySpace=%s;Compression=Snappy" keyspace).Result
    let queries = Queries.CreateInstance(session).Result
    (session, queries)

let assertEmptyAndIgnore s =
    if Seq.isEmpty s then ()
    else failwith "Not empty"

let readYesNo prompt =
    printf "%s [Y/n] " prompt
    Seq.initInfinite (fun _ ->
        match Console.ReadLine().ToLower() with
        | "y" -> Some true
        | "n" -> Some false
        | _ ->
            printf "Invalid response, please retry [Y/n] "
            None
    ) |> Seq.pick id