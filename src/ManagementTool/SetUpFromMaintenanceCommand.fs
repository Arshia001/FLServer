[<AutoOpen>]
module SetUpFromMaintenanceCommand

open CommandLine

[<Verb("up", HelpText = "Bring service back up from maintenance, allowing players to connect")>]
type UpFromMaintenance = {
    [<Option('k', "keyspace", HelpText = "Keyspace name; defaults to 'fl'", Default = "fl")>] keyspace: string
}

let runUpFromMaintenance (cmd: UpFromMaintenance) =
    ()