[<AutoOpen>]
module UpdateConfigCommand

open CommandLine
open FLGrainInterfaces.Configuration
open System.IO

[<Verb("update-config", HelpText = "Update config from database, or optionally from a JSON document")>]
type UpdateConfig = {
    [<Option('f', "input-file", HelpText = "The json file to use; leave out to update from database")>] jsonConfig: string option
    [<Option("avatar", HelpText = "Update avatar config data")>] avatar: bool
    [<Option("db", HelpText = "Directly update config in database")>] database: bool
    [<Option('k', "keyspace", HelpText = "Keyspace name; if left out, will use keyspace specified in mgmtool.keyspace. Only relevant when --db is specified.")>] keyspace: string option
}

let runUpdateConfig (cmd: UpdateConfig) =
    match cmd.database, cmd.jsonConfig with
    | false, Some file ->
        if not <| File.Exists file then raise (ToolFinished <| sprintf "Cannot find file '%s'" file)

        let configText = File.ReadAllText file
        use client = buildOrleansClient ()
        if cmd.avatar then
            client.GetGrain<ISystemConfig>(0L).UploadAvatarConfig(configText) |> runSynchronously
        else
            client.GetGrain<ISystemConfig>(0L).UploadConfig(configText) |> runSynchronously
        raise <| ToolFinished "Config updated successfully"
    | true, Some file ->
        if not <| promptYesNo "Manually uploading config data to the database is EXTREMELY DANGEROUS and should only be attempted when the server is down for mainetance. Continue?" then
            raise <| ToolFinished "Canceled"

        let keyspace = getKeyspace cmd.keyspace
        let session, queries = buildCassandraSession keyspace

        if not <| File.Exists file then raise (ToolFinished <| sprintf "Cannot find file '%s'" file)

        let configText = File.ReadAllText file

        executeNonQuery session <| queries.["fl_updateConfig"].Bind({| data = configText; key = if cmd.avatar then "avatar" else "config" |});
        
        raise <| ToolFinished "Config updated successfully in database"
    | true, None ->
        raise <| ToolFinished "Must specify input file if --db is specified"
    | false, None when cmd.avatar ->
        raise <| ToolFinished "Must specify input file if --avatar is specified"
    | false, None ->
        use client = buildOrleansClient ()
        client.GetGrain<ISystemConfig>(0L).UpdateConfigFromDatabase() |> runSynchronously
        raise <| ToolFinished "Config updated successfully"

let runUpdateFromDatabase () = runUpdateConfig { database = false; avatar = false; keyspace = None; jsonConfig = None }

let promptAndRunUpdateFromDatabase () =
    if promptYesNo "Do you also wish to update the server's config from database now? (you can do this later by running mgmtool update-config)" then
        runUpdateFromDatabase ()