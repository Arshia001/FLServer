[<AutoOpen>]
module ReadConfigCommand

open CommandLine
open System.IO

[<Verb("read-config", HelpText = "Read config from database into a JSON document")>]
type ReadConfig = {
    [<Option('f', "output-file", HelpText = "The json file to write to", Required = true)>] file: string
    [<Option("avatar", HelpText = "Read avatar config data")>] avatar: bool
    [<Option('k', "keyspace", HelpText = "Keyspace name; if left out, will use keyspace specified in mgmtool.keyspace. Only relevant when --db is specified.")>] keyspace: string option
}

let runReadConfig (cmd: ReadConfig) =
    let keyspace = getKeyspace cmd.keyspace
    let session, queries = buildCassandraSession keyspace

    let configText = executeSingleRow session <| queries.["fl_readConfig"].Bind({| key = if cmd.avatar then "avatar" else "config" |})

    File.WriteAllText(cmd.file, configText.Value.["data"] :?> string)

    printfn "Done"
