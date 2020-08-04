[<AutoOpen>]
module WordSuggestionGiftCommand

open CommandLine
open FLGrainInterfaces
open System
open Orleans
open System.IO

[<Verb("give-word-gift", HelpText = "Give coin gift to a player for suggesting words")>]
type WordSuggestionGift = {
    [<Option('i', "player-id", HelpText = "Player ID", SetName = "manual", Required = true)>] playerID: Guid option
    [<Option('c', "category-name", HelpText = "Name of category for which words were suggested", SetName = "manual", Required = true)>] categoryName: string option
    [<Option('w', "word", HelpText = "Accepted words, may specify more than one", SetName = "manual", Required = true, Min = 1)>] words: string seq
    [<Option('a', "coin-amount", HelpText = "Number of coins to give", SetName = "manual", Required = true)>] coinAmount: uint32 option
    [<Option('f', "file", HelpText = "Name of CSV file to read gift information from, with the following columns: id, coin amount, category name, word, word...", SetName = "batch", Required = true)>] filePath: string option
}

let private giveOne (client: IClusterClient, playerID: Guid, amount: uint32, categoryName: string, words: string seq) =
    let gift = CoinGiftInfo(CoinGiftSubject.SuggestedWords, amount, null, Nullable(), categoryName, words |> String.concat "|", null, null)
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
                let result = giveOne(client.Value, id, uint32 amount, parts.[2], parts |> Seq.skip 3)
                if result then printfn "Line %i rewarded successfully" i else printfn "Failed to reward line %i as no such player found" i
            | _ -> printfn "Failed to parse line %i due to invalid values: %s" i l
        )

let runWordSuggestionGift (cmd: WordSuggestionGift) =
    let client = lazy buildOrleansClient ()

    try
        match cmd.filePath with
        | Some f -> giveAllFromFile (client, f)
        | None ->
            match cmd.playerID, cmd.coinAmount, cmd.categoryName, not <| Seq.isEmpty cmd.words with
            | Some id, Some amount, Some category, true ->
                let result = giveOne (client.Value, id, amount, category, cmd.words)
                (if result then "Success" else "No such player found") |> ToolFinished |> raise
            | _ -> ToolFinished "Invalid command-line arguments" |> raise
    finally
        if client.IsValueCreated then client.Value.Dispose()