[<AutoOpen>]
module UpdateGroupsCommand

open CommandLine
open System.IO
open System.Collections.Generic
open Cassandra
open OrleansCassandraUtils.Utils

[<Verb("update-groups", HelpText = "Update groups from a given JSON file")>]
type UpdateGroups = {
    [<Option('k', "keyspace", HelpText = "Keyspace name; if left out, will use keyspace specified in mgmtool.keyspace")>] keyspace: string option
    [<Option("dry-run", HelpText = "Perform a dry run, only printing changes")>] dryRun: bool
    [<Option("only-changed", SetName = "only-changed", HelpText = "Apply changed groups only")>] changesOnly: bool
    [<Option("only-new", SetName = "only-new", HelpText = "Apply new groups only")>] newOnly: bool
    [<Option("all", SetName = "all", HelpText = "Apply all groups regardless of changed/new status")>] applyAll: bool
    [<Option('f', "input-file", HelpText = "JSON file to read groups from", Required = true)>] file: string
}

let printGroups title (groups: GroupEntry seq) =
    if Seq.isEmpty groups |> not then
        groups
        |> Seq.map (fun c -> "    " + c.Name)
        |> String.concat "\n"
        |> printfn "%s:\n%s" title
    else
        printfn "%s: None" title

let applyChangeSet (session: ISession, queries: Queries) (changeSet: GroupEntry seq) =
    let query = queries.["fl_upsertGroup"]

    changeSet
        |> Seq.iter (fun c -> 
            printfn "Applying %s" c.Name

            query.Bind({| name = c.Name; id = c.ID |})
                |> session.Execute
                |> assertEmptyAndIgnore
        )

let runUpdateGroups (cmd: UpdateGroups) =
    let fileContents = cmd.file |> File.ReadAllText |> GroupCategoryData.Parse

    let keyspace = getKeyspace cmd.keyspace

    let session, queries = buildCassandraSession keyspace

    let existingGroups =
        queries.["fl_readGroups"].Bind()
        |> session.Execute
        |> Seq.map (fun r -> 
            let id = r.["id"] :?> int
            id, { 
                ID = id
                Name = r.["name"] :?> string
            })
        |> Map.ofSeq

    let providedGroups = fileContents.Groups |> Seq.map GroupEntry.ofFileData |> List.ofSeq

    let oldGroups, newGroups = providedGroups |> List.partition (fun c -> existingGroups |> Map.containsKey c.ID)

    let sameGroups, updatedGroups = oldGroups |> List.partition (fun c -> c = existingGroups.[c.ID])

    printGroups "New groups" newGroups
    printGroups "Updated groups" updatedGroups
    printGroups "Groups with no changes" sameGroups

    if cmd.dryRun then raise <| ToolFinished "Dry run requested, stopping"

    let message, changeSet =
        if cmd.newOnly then
            sprintf "Will apply %i new groups, continue?" <| List.length newGroups,
            Seq.ofList newGroups
        else if cmd.changesOnly then
            sprintf "Will apply %i changed groups, continue?" <| List.length updatedGroups,
            Seq.ofList updatedGroups
        else if cmd.applyAll then
            sprintf "Will apply all %i groups, continue?" <| List.length providedGroups,
            Seq.ofList providedGroups
        else
            sprintf "Will apply %i new and %i changed groups, continue?" (List.length newGroups) (List.length updatedGroups),
            Seq.append newGroups updatedGroups

    if Seq.isEmpty changeSet then raise <| ToolFinished "No changes to process"

    if not <| promptYesNo message then raise <| ToolFinished "Cancelled"

    applyChangeSet (session, queries) changeSet

    printfn "Done"

    promptAndRunUpdateFromDatabase ()