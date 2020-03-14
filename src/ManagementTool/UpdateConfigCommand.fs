[<AutoOpen>]
module UpdateConfigCommand

open CommandLine
open FLGrainInterfaces.Configuration
open System.IO

[<Verb("update-config", HelpText = "Update config from database, or optionally from a JSON document")>]
type UpdateConfig = {
    [<Option('f', "json-file", HelpText = "The json file to use; leave out to update from database")>] jsonConfig: string option
}

let runUpdateConfig (cmd: UpdateConfig) =
    let client = lazy buildOrleansClient ()

    match cmd.jsonConfig with
    | Some file ->
        if not <| File.Exists file then
            printfn "Cannot find file '%s'" file
        else
            let configText = File.ReadAllText file
            client.Value.GetGrain<ISystemConfig>(0L).UploadConfig(configText) |> runSynchronously
            printfn "Config updated successfully"
    | None ->
        client.Value.GetGrain<ISystemConfig>(0L).UpdateConfigFromDatabase() |> runSynchronously
        printfn "Config updated successfully"

    ()