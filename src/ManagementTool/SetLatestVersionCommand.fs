[<AutoOpen>]
module SetLatestVersionCommand

open CommandLine

[<Verb("set-latest-version", HelpText = "Update the latest client version configuration")>]
type SetLatestVersion = {
    [<Option('k', "keyspace", HelpText = "Keyspace name; if left out, will use keyspace specified in mgmtool.keyspace")>] keyspace: string option
    [<Value(0, MetaName = "Version", HelpText = "Version to set")>] version: int
}

let runSetLatestVersion (cmd: SetLatestVersion) =
    let keyspace = getKeyspace cmd.keyspace

    let (session, queries) = buildCassandraSession keyspace

    let configRow =
        queries.["fl_readConfig"].Bind({| key = "latest-version" |})
        |> executeSingleRow session

    let version = 
        configRow
        |> Option.bind (fun r ->
            tryParseInt r.["data"]
        )

    let versionString =
        version
        |> Option.map string
        |> Option.defaultValue "<UNAVAILABLE>"

    if
        sprintf "Current latest version is %s, update to %i?" versionString cmd.version
        |> promptYesNo
        |> not
    then
        raise <| ToolFinished "Cancelled"

    match version with
    | Some v ->
        if v > cmd.version then
            if
                sprintf "WARNING: the new version is older than the existing value. Do you wish to continue?"
                |> promptYesNo
                |> not
            then
                raise <| ToolFinished "Cancelled"
    | _ -> ()

    queries.["fl_updateConfig"].Bind({| key = "latest-version"; data = string cmd.version |})
        |> session.Execute
        |> assertEmptyAndIgnore

    printfn "Done"

    promptAndRunUpdateFromDatabase ()