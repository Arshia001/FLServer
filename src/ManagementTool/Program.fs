// Learn more about F# at http://fsharp.org

open System
open CommandLine

let parse = 
    Parser.Default.ParseArguments<
        UpdateConfig,
        ReadConfig,
        DownForMaintenance, 
        UpFromMaintenance, 
        RenameCategory,
        SetVersion,
        UpdateCategories,
        UpdateGroups,
        UpdateAll,
        ConnectedClients,
        DeleteStorage,
        WordSuggestionGift,
        CategorySuggestionGift,
        ReadSuggestions,
        GetPlayerID,
        GenericGift
    >

[<EntryPoint>]
let main argv =
    try
        match parse argv with
        | :? Parsed<obj> as p ->
            match p.Value with
            | :? UpdateConfig as cmd -> runUpdateConfig cmd
            | :? ReadConfig as cmd -> runReadConfig cmd
            | :? DownForMaintenance as cmd -> runDownForMaintenance cmd
            | :? UpFromMaintenance as cmd -> runUpFromMaintenance cmd
            | :? RenameCategory as cmd -> runRenameCategory cmd
            | :? SetVersion as cmd -> runSetVersion cmd
            | :? UpdateCategories as cmd -> runUpdateCategories cmd
            | :? UpdateGroups as cmd -> runUpdateGroups cmd
            | :? UpdateAll as cmd -> runUpdateAll cmd
            | :? ConnectedClients as cmd -> runConnectedClients cmd
            | :? DeleteStorage as cmd -> runDeleteStorage cmd
            | :? WordSuggestionGift as cmd -> runWordSuggestionGift cmd
            | :? CategorySuggestionGift as cmd -> runCategorySuggestionGift cmd
            | :? ReadSuggestions as cmd -> runReadSuggestions cmd
            | :? GetPlayerID as cmd -> runGetPlayerID cmd
            | :? GenericGift as cmd -> runGenericGift cmd
            | _ -> printfn "Unknown command type %A" p.Value
            0
        | :? NotParsed<obj> as np ->
            match np.Errors with
            | e when e.IsHelp() || e.IsVersion() -> ()
            | _ -> printfn "Invalid command line options"
            -1
        | _ as e -> 
            printfn "Parser failure: %A" e
            -1
    with 
    | ToolFinished msg ->
        printfn "%s" msg
        -1
    | e ->
        printfn "\n\nInternal error: %A" e
        -1
