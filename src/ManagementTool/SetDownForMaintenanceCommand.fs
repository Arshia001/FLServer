[<AutoOpen>]
module SetDownForMaintenanceCommand

open CommandLine

[<Verb("down", HelpText = "Mark service as down for maintenance, blocking new players from connecting")>]
type DownForMaintenance = {
    [<Option('k', "keyspace", HelpText = "Keyspace name; if left out, will use keyspace specified in mgmtool.keyspace")>] keyspace: string option
}

let runDownForMaintenance (cmd: DownForMaintenance) =
    let keyspace = getKeyspace cmd.keyspace
    if 
        sprintf "Are you sure you wish to bring the server in keyspace %s down?" keyspace
            |> promptYesNo
            |> not
    then
        raise <| ToolFailureException "Cancelled"

    let (session, queries) = buildCassandraSession keyspace
    let statement = queries.["fl_updateConfig"].Bind({| key = "maintenance-status"; data = "in-progress" |})
    session.Execute(statement) |> assertEmptyAndIgnore
    printfn "Done"
