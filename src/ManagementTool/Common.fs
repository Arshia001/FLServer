[<AutoOpen>]
module Common

open System
open Microsoft.Extensions.Logging
open Orleans
open System.Threading.Tasks
open OrleansCassandraUtils.Utils
open System.IO
open System.Collections.Generic

exception ToolFinished of message : string

module Seq =
    let equalBy (f: 'a -> 'a -> bool) (s1: 'a seq) (s2: 'a seq) =
        let e1 = s1.GetEnumerator()
        let e2 = s2.GetEnumerator()
        let mutable stop = false
        let mutable result = true
        while not stop do
            let m1 = e1.MoveNext()
            let m2 = e2.MoveNext()
            if (m1 && not m2) || (not m1 && m2) then
                stop <- true
                result <- false
            else if not m1 && not m2 then
                stop <- true
            else if not <| f e1.Current e2.Current then
                stop <- true
                result <- false
        result

    let equal s1 s2 = equalBy (=) s1 s2

module Dictionary =
    let equalBy (valueEq: 'b -> 'b -> bool) (d1: IDictionary<'a, 'b>) (d2: IDictionary<'a, 'b>) =
        if d1.Count <> d2.Count then false
        else
            let valueEq = OptimizedClosures.FSharpFunc<_,_,_>.Adapt valueEq
            d1.Keys |> Seq.forall(fun k -> 
                match d2.TryGetValue k with
                | true, v -> valueEq.Invoke(v, d1.[k])
                | _ -> false
            )

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

let executeSingleRow (session: Cassandra.ISession) (statement: Cassandra.IStatement) =
    session.Execute statement |> Seq.tryExactlyOne

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
        | "c" -> raise <| ToolFinished "Cancelled"
        | _ ->
            printf "Invalid response, please retry [y/n/c] "
            None
    ) |> Seq.pick id

let getKeyspace providedValue =
    providedValue 
    |> Option.defaultWith (fun () ->
        if File.Exists "mgmtool.keyspace" then (File.ReadAllText "mgmtool.keyspace").Trim()
        else raise <| ToolFinished "No keyspace specified on command line and no mgmtool.keyspace file in current directory"
    )

let tryParseInt (o: obj) =
    if o = null then
        None
    else
        match string o |> Int32.TryParse with
        | (true, i) -> Some i
        | (false, _) -> None