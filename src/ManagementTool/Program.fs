// Learn more about F# at http://fsharp.org

open System
open CommandLine


[<EntryPoint>]
let main argv =
    match Parser.Default.ParseArguments<UpdateConfig, DownForMaintenance, UpFromMaintenance> argv with
    | :? Parsed<obj> as p ->
        match p.Value with
        | :? UpdateConfig as cmd -> runUpdateConfig cmd
        | :? DownForMaintenance as cmd -> runDownForMaintenance cmd
        | :? UpFromMaintenance as cmd -> runUpFromMaintenance cmd
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
