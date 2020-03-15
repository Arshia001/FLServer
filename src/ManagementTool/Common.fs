[<AutoOpen>]
module Common

open System
open Microsoft.Extensions.Logging
open Orleans
open System.Threading.Tasks
open OrleansCassandraUtils.Utils
open System.IO

exception ToolFailureException of message : string

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

let promptYesNo prompt =
    printf "%s [y/n] " prompt
    Seq.initInfinite (fun _ ->
        match Console.ReadLine().ToLower() with
        | "y" -> Some true
        | "n" -> Some false
        | _ ->
            printf "Invalid response, please retry [y/n] "
            None
    ) |> Seq.pick id

let promptYesNoCancel prompt =
    printf "%s [y/n/c] " prompt
    Seq.initInfinite (fun _ ->
        match Console.ReadLine().ToLower() with
        | "y" -> Some true
        | "n" -> Some false
        | "c" -> raise <| ToolFailureException "Cancelled"
        | _ ->
            printf "Invalid response, please retry [y/n/c] "
            None
    ) |> Seq.pick id

let getKeyspace providedValue =
    providedValue 
    |> Option.defaultWith (fun () ->
        if File.Exists "mgmtool.keyspace" then File.ReadAllText "mgmtool.keyspace"
        else raise <| ToolFailureException "No keyspace specified on command line and no mgmtool.keyspace file in current directory"
    )