[<AutoOpen>]
module WordSuggestionGiftCommand

open CommandLine
open FLGrainInterfaces
open System

[<Verb("give-word-gift", HelpText = "Give coin gift to a player for suggesting words")>]
type WordSuggestionGift = {
    [<Option('i', "player-id", HelpText = "Player ID", Required = true)>] playerID: Guid
    [<Option('c', "category-name", HelpText = "Name of category for which words were suggested", Required = true)>] categoryName: string
    [<Option('w', "word", HelpText = "Accepted words, may specify more than one", Min = 1)>] words: string seq
    [<Option('a', "coin-amount", HelpText = "Number of coins to give", Required = true)>] coinAmount: uint32
}

let runWordSuggestionGift (cmd: WordSuggestionGift) =
    let gift = CoinGiftInfo(Guid.NewGuid(), CoinGiftSubject.SuggestedWords, cmd.coinAmount, null, Nullable(), cmd.categoryName, cmd.words |> String.concat "|", null, null)

    use client = buildOrleansClient ()

    let result = client.GetGrain<IPlayer>(cmd.playerID).ReceiveCoinGift(gift).Result

    (if result then "Success" else "No such player found") |> ToolFinished |> raise
