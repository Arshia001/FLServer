[<AutoOpen>]
module Common

open System
open Microsoft.Extensions.Logging
open Orleans
open System.Threading.Tasks
open OrleansCassandraUtils.Utils
open System.IO
open System.Collections.Generic
open System.Diagnostics
open System.Runtime.InteropServices

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

    let iterInterleaved (fInterleave: unit -> unit) (f: 'a -> unit) (s: 'a seq) =
        let e = s.GetEnumerator()
        let mutable first = true
        while e.MoveNext() do
            if first then
                first <- false
            else
                fInterleave ()

            f e.Current

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

let executeSingleRow (session: Cassandra.ISession) (statement: Cassandra.IStatement) =
    session.Execute statement |> Seq.tryExactlyOne

let executeNonQuery (session: Cassandra.ISession) (statement: Cassandra.IStatement) =
    session.Execute statement |> assertEmptyAndIgnore

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
        | "c" -> raise <| ToolFinished "Canceled"
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

let tryParseGuid (o: obj) =
    if o = null then
        None
    else
        match string o |> Guid.TryParse with
        | (true, i) -> Some i
        | (false, _) -> None

let exec (executable: string) (arguments: string) =
    let proc = Process.Start(executable, arguments)
    proc.WaitForExit ()
    if proc.ExitCode <> 0 then
        sprintf "Failed: process %s with arguments '%s' failed with exit code %i" executable arguments proc.ExitCode |> ToolFinished |> raise

let runNodeTool =
    let toolName = if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then "nodetool.bat" else "nodetool"
    exec toolName

let takeSnapshot keyspace table snapshotName =
    runNodeTool <| sprintf "snapshot %s -cf %s -t %s" keyspace table snapshotName
