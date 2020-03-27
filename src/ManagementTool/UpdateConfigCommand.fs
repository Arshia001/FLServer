[<AutoOpen>]
module UpdateConfigCommand

open CommandLine
open FLGrainInterfaces.Configuration
open System.IO

[<Verb("update-config", HelpText = "Update config from database, or optionally from a JSON document")>]
type UpdateConfig = {
    [<Option('f', "input-file", HelpText = "The json file to use; leave out to update from database")>] jsonConfig: string option
}

let runUpdateConfig (cmd: UpdateConfig) =
    match cmd.jsonConfig with
    | Some file ->
        if not <| File.Exists file then raise (ToolFinished <| sprintf "Cannot find file '%s'" file)

        let configText = File.ReadAllText file
        use client = buildOrleansClient ()
        client.GetGrain<ISystemConfig>(0L).UploadConfig(configText) |> runSynchronously
        raise <| ToolFinished "Config updated successfully"
    | None ->
        use client = buildOrleansClient ()
        client.GetGrain<ISystemConfig>(0L).UpdateConfigFromDatabase() |> runSynchronously
        raise <| ToolFinished "Config updated successfully"

let runUpdateFromDatabase () = runUpdateConfig { jsonConfig = None }

let promptAndRunUpdateFromDatabase () =
    if promptYesNo "Do you also wish to update the server's config from database now? (you can do this later by running mgmtool update-config)" then
        runUpdateFromDatabase ()