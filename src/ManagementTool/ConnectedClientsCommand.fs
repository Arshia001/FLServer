[<AutoOpen>]
module ConnectedClientsCommand

open CommandLine
open FLGrainInterfaces.ServerStatistics

[<Verb("connected-clients", HelpText = "Get total number of connected clients from all servers")>]
type ConnectedClients() = class end

let runConnectedClients (_: ConnectedClients) =
    let client = buildOrleansClient ()

    let count = client.GetGrain<IServerStatistics>(0L).GetConnectedClientCount().Result

    printfn "Connected clients across all server(s): %i" count