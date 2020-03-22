[<AutoOpen>]
module DeleteStorageCommand

open System
open CommandLine
open Cassandra

[<Verb("delete-storage", HelpText = "Bulk delete rows from storage")>]
type DeleteStorage = {
    [<Option('k', "keyspace", HelpText = "Keyspace name; if left out, will use keyspace specified in mgmtool.keyspace")>] keyspace: string option
    [<Option('c', "condition", HelpText = "The condition to match rows against for deletion, must be valid CQL")>] condition: string
}

let toHex (b: byte array) = "0x" + BitConverter.ToString(b).Replace("-", "")

let printRows (rows: Row seq) =
    for row in rows do
        printfn "type: %s, id: %s" (string row.["grain_type"]) (row.["grain_id"] :?> byte array |> toHex)

//?? This could be parameterized to work on all tables
let runDeleteStorage (cmd: DeleteStorage) =
    let keyspace = getKeyspace cmd.keyspace
    let session, queries = buildCassandraSession keyspace

    let query = sprintf "SELECT grain_type, grain_id FROM storage WHERE %s ALLOW FILTERING" cmd.condition
    let rows = session.Execute query |> Seq.toArray

    let count = Array.length rows

    if count = 0 then
        raise <| ToolFinished "No matching rows found"

    let printCount = min count 10

    printfn "Will delete %i rows, %i of which are displayed below:" count printCount
    rows |> Seq.take printCount |> printRows

    if not <| promptYesNo "Are you sure?" then
        raise <| ToolFinished "Canceled"

    if promptYesNo "Do you wish to take a snapshot of the storage table first?" then
        let now = DateTime.Now
        takeSnapshot keyspace "storage" <| sprintf "before-delete-command-%i-%i-%i-%i-%i" now.Year now.Month now.Day now.Hour now.Minute

    printfn "Deleting rows..."

    let deleteStatement = session.Prepare("DELETE FROM storage WHERE grain_type = :grain_type AND grain_id = :grain_id")
    for row in rows do
        deleteStatement.Bind({| grain_type = row.["grain_type"]; grain_id = row.["grain_id"] |})
            |> session.Execute
            |> assertEmptyAndIgnore

    printfn "Done"
