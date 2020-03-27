[<AutoOpen>]
module ConnectedClientsCommand

open CommandLine
open FLGrainInterfaces.ServerStatistics

[<Verb("connected-clients", HelpText = "Get total number of connected clients from all servers")>]
type ConnectedClients() = class end

let runConnectedClients (_: ConnectedClients) =
    use client = buildOrleansClient ()

    let count = client.GetGrain<IServerStatistics>(0L).GetConnectedClientCount().Result

    sprintf "Connected clients across all server(s): %i" count |> ToolFinished |> raise