// Learn more about F# at http://fsharp.org

open System
open CommandLine

let parse = 
    Parser.Default.ParseArguments<
        UpdateConfig, 
        DownForMaintenance, 
        UpFromMaintenance, 
        RenameCategory,
        SetLatestVersion
    >

[<EntryPoint>]
let main argv =
    try
        match parse argv with
        | :? Parsed<obj> as p ->
            match p.Value with
            | :? UpdateConfig as cmd -> runUpdateConfig cmd
            | :? DownForMaintenance as cmd -> runDownForMaintenance cmd
            | :? UpFromMaintenance as cmd -> runUpFromMaintenance cmd
            | :? RenameCategory as cmd -> runRenameCategory cmd
            | :? SetLatestVersion as cmd -> runSetLatestVersion cmd
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
    | ToolFailureException msg ->
        printfn "%s" msg
        -1
    | e ->
        printfn "Internal error: %A" e
        -1