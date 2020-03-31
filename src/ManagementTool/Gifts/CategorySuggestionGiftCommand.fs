[<AutoOpen>]
module CategorySuggestionGiftCommand

open CommandLine
open FLGrainInterfaces
open System

[<Verb("give-category-gift", HelpText = "Give coin gift to a player for suggesting categories")>]
type CategorySuggestionGift = {
    [<Option('i', "player-id", HelpText = "Player ID", Required = true)>] playerID: Guid
    [<Option('c', "category-name", HelpText = "Name of category for which words were suggested", Required = true)>] categoryName: string
    [<Option('a', "coin-amount", HelpText = "Number of coins to give", Required = true)>] coinAmount: uint32
}

let runCategorySuggestionGift (cmd: CategorySuggestionGift) =
    let gift = CoinGiftInfo(Guid.NewGuid(), CoinGiftSubject.SuggestedCategories, cmd.coinAmount, null, Nullable(), cmd.categoryName, null, null, null)

    use client = buildOrleansClient ()

    let result = client.GetGrain<IPlayer>(cmd.playerID).ReceiveCoinGift(gift).Result

    (if result then "Success" else "No such player found") |> ToolFinished |> raise
