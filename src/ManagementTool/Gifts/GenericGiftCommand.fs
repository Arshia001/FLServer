[<AutoOpen>]
module GenericGiftCommand

open CommandLine
open FLGrainInterfaces
open System
open Orleans
open System.IO

[<Verb("give-gift", HelpText = "Give coin gift with custom message to a player")>]
type GenericGift = {
    [<Option('i', "player-id", HelpText = "Player ID", SetName = "manual", Required = true)>] playerID: Guid option
    [<Option('m', "message", HelpText = "Message to display to user", SetName = "manual", Required = true)>] message: string option
    [<Option('a', "coin-amount", HelpText = "Number of coins to give", SetName = "manual", Required = true)>] coinAmount: uint32 option
    [<Option('f', "file", HelpText = "Name of CSV file to read gift information from, with the following columns: id, coin amount, message", SetName = "batch", Required = true)>] filePath: string option
}

let private giveOne (client: IClusterClient, playerID: Guid, amount: uint32, message: string) =
    let gift = CoinGiftInfo(CoinGiftSubject.GiftToAll, amount, message, Nullable(), null, null, null, null)
    client.GetGrain<IPlayer>(playerID).ReceiveCoinGift(gift).Result

let private giveAllFromFile (client: Lazy<IClusterClient>, f: string) =
    File.ReadAllLines(f)
    |> Seq.indexed
    |> Seq.iter (fun (i, l) -> 
        let parts = l.Split(',')
        if Array.length parts < 4 then printfn "Failed to parse line %i since there are too few values: %s" i l
        else
            let id = tryParseGuid parts.[0]
            let amount = tryParseInt parts.[1]
            match id, amount with
            | Some id, Some amount ->
                let result = giveOne(client.Value, id, uint32 amount, parts.[2])
                if result then printfn "Line %i rewarded successfully" i else printfn "Failed to reward line %i as no such player found" i
            | _ -> printfn "Failed to parse line %i due to invalid values: %s" i l
        )

let runGenericGift (cmd: GenericGift) =
    let client = lazy buildOrleansClient ()

    try
        match cmd.filePath with
        | Some f -> giveAllFromFile (client, f)
        | None ->
            match cmd.playerID, cmd.coinAmount, cmd.message with
            | Some id, Some amount, Some message ->
                let result = giveOne (client.Value, id, amount, message)
                (if result then "Success" else "No such player found") |> ToolFinished |> raise
            | _ -> ToolFinished "Invalid command-line arguments" |> raise
    finally
        if client.IsValueCreated then client.Value.Dispose()