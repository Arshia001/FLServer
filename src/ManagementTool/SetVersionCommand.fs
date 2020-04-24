[<AutoOpen>]
module SetVersionCommand

open CommandLine

[<Verb("set-version", HelpText = "Update the latest client version configuration")>]
type SetVersion = {
    [<Option('k', "keyspace", HelpText = "Keyspace name; if left out, will use keyspace specified in mgmtool.keyspace")>] keyspace: string option
    [<Value(0, MetaName = "Version type", HelpText = "Version type to set; One of latest (l), last-compatible (c)")>] versionType: string
    [<Value(1, MetaName = "Version", HelpText = "Version to set")>] version: int
}

let runSetVersion (cmd: SetVersion) =
    let versionKey =
        match cmd.versionType with
        | "l" | "latest" -> "latest-version"
        | "c" | "last-compatible" -> "last-compatible-version"
        | _ -> raise <| ToolFinished "Invalid version type specified"

    let keyspace = getKeyspace cmd.keyspace

    let (session, queries) = buildCassandraSession keyspace

    let configRow =
        queries.["fl_readConfig"].Bind({| key = versionKey |})
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
        sprintf "Current version is %s, update to %i?" versionString cmd.version
        |> promptYesNo
        |> not
    then
        raise <| ToolFinished "Canceled"

    match version with
    | Some v ->
        if v > cmd.version then
            if
                sprintf "WARNING: the new version is older than the existing value. Do you wish to continue?"
                |> promptYesNo
                |> not
            then
                raise <| ToolFinished "Canceled"
        else if v = cmd.version then
            raise <| ToolFinished "Already at this version"
    | _ -> ()

    queries.["fl_updateConfig"].Bind({| key = versionKey; data = string cmd.version |})
        |> session.Execute
        |> assertEmptyAndIgnore

    printfn "Done"

    promptAndRunUpdateFromDatabase ()