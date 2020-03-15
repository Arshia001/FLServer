[<AutoOpen>]
module UpdateAllCommand

open CommandLine
open System.IO
open System.Collections.Generic
open Cassandra
open OrleansCassandraUtils.Utils

[<Verb("update-all", HelpText = "Update groups and categories from a given JSON file")>]
type UpdateAll = {
    [<Option('k', "keyspace", HelpText = "Keyspace name; if left out, will use keyspace specified in mgmtool.keyspace")>] keyspace: string option
    [<Option("dry-run", HelpText = "Perform a dry run, only printing changes")>] dryRun: bool
    [<Option("only-changed", SetName = "only-changed", HelpText = "Apply changed groups and categories only")>] changesOnly: bool
    [<Option("only-new", SetName = "only-new", HelpText = "Apply new groups and categories only")>] newOnly: bool
    [<Option("all", SetName = "all", HelpText = "Apply all groups and categories regardless of changed/new status")>] applyAll: bool
    [<Option('f', "input-file", HelpText = "JSON file to read groups and categories from", Required = true)>] file: string
}

let runUpdateAll (cmd: UpdateAll) =
    printfn "Updating groups..."
    let updateGroups = {
        UpdateGroups.applyAll = cmd.applyAll
        UpdateGroups.changesOnly = cmd.changesOnly
        UpdateGroups.dryRun = cmd.dryRun
        UpdateGroups.file = cmd.file
        UpdateGroups.keyspace = cmd.keyspace
        UpdateGroups.newOnly = cmd.newOnly
    }
    try
        runUpdateGroups updateGroups
    with ToolFinished msg -> printfn "%s" msg

    printfn ""
    printfn "Updating categories..."
    let updateCategories = {
        UpdateCategories.applyAll = cmd.applyAll
        UpdateCategories.changesOnly = cmd.changesOnly
        UpdateCategories.dryRun = cmd.dryRun
        UpdateCategories.file = cmd.file
        UpdateCategories.keyspace = cmd.keyspace
        UpdateCategories.newOnly = cmd.newOnly
    }
    try
        runUpdateCategories updateCategories
    with ToolFinished msg -> printfn "%s" msg

    printfn ""
    printfn "Done"
