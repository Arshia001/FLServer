[<AutoOpen>]
module SetDownForMaintenanceCommand

open CommandLine

[<Verb("down", HelpText = "Mark service as down for maintenance, blocking new players from connecting")>]
type DownForMaintenance = {
    [<Option('k', "keyspace", HelpText = "Keyspace name; defaults to 'fl'", Default = "fl")>] keyspace: string
}

let runDownForMaintenance (cmd: DownForMaintenance) =
    ()