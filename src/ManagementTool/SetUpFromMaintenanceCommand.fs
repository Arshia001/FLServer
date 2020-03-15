[<AutoOpen>]
module SetUpFromMaintenanceCommand

open CommandLine

[<Verb("up", HelpText = "Bring service back up from maintenance, allowing players to connect")>]
type UpFromMaintenance = {
    [<Option('k', "keyspace", HelpText = "Keyspace name; if left out, will use keyspace specified in mgmtool.keyspace")>] keyspace: string option
}

let runUpFromMaintenance (cmd: UpFromMaintenance) =
    let keyspace = getKeyspace cmd.keyspace
    if 
        sprintf "Are you sure you wish to bring the server in keyspace %s back up?" keyspace
            |> promptYesNo
            |> not
    then
        raise <| ToolFailureException "Cancelled"

    let (session, queries) = buildCassandraSession keyspace
    let statement = queries.["fl_updateConfig"].Bind({| key = "maintenance-status"; data = "none" |})
    session.Execute(statement) |> assertEmptyAndIgnore
    printfn "Done"
