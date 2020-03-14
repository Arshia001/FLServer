[<AutoOpen>]
module SetUpFromMaintenanceCommand

open CommandLine

[<Verb("up", HelpText = "Bring service back up from maintenance, allowing players to connect")>]
type UpFromMaintenance = {
    [<Option('k', "keyspace", HelpText = "Keyspace name; defaults to 'fl'", Default = "fl")>] keyspace: string
}

let runUpFromMaintenance (cmd: UpFromMaintenance) =
    let (session, queries) = buildCassandraSession cmd.keyspace
    let statement = queries.["fl_updateConfig"].Bind({| key = "maintenance-status"; data = "none" |})
    session.Execute(statement) |> ignore
    printfn "Done"
