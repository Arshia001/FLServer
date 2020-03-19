[<AutoOpen>]
module UpdateCategoriesCommand

open CommandLine
open System.IO
open System.Collections.Generic
open Cassandra
open OrleansCassandraUtils.Utils

[<Verb("update-categories", HelpText = "Update categories from a given JSON file")>]
type UpdateCategories = {
    [<Option('k', "keyspace", HelpText = "Keyspace name; if left out, will use keyspace specified in mgmtool.keyspace")>] keyspace: string option
    [<Option("dry-run", HelpText = "Perform a dry run, only printing changes")>] dryRun: bool
    [<Option("only-changed", SetName = "only-changed", HelpText = "Apply changed categories only")>] changesOnly: bool
    [<Option("only-new", SetName = "only-new", HelpText = "Apply new categories only")>] newOnly: bool
    [<Option("all", SetName = "all", HelpText = "Apply all categories regardless of changed/new status")>] applyAll: bool
    [<Option('f', "input-file", HelpText = "JSON file to read categories from", Required = true)>] file: string
}

let printCategories title (categories: CategoryEntry seq) =
    if Seq.isEmpty categories |> not then
        categories
        |> Seq.map (fun c -> "    " + c.Name)
        |> String.concat "\n"
        |> printfn "%s:\n%s" title
    else
        printfn "%s: None" title

let printNames title (names: string seq) =
    if Seq.isEmpty names |> not then
        names
        |> Seq.map (fun n -> "    " + n)
        |> String.concat "\n"
        |> printfn "%s:\n%s" title
    else
        printfn "%s: None" title

let applyChangeSet (session: ISession, queries: Queries) (changeSet: CategoryEntry seq) =
    let query = queries.["fl_upsertCategory"]

    changeSet
        |> Seq.iter (fun c -> 
            printfn "Applying %s" c.Name

            query.Bind({| name = c.Name; group_id = c.GroupID; words = c.Words |})
                |> session.Execute
                |> assertEmptyAndIgnore
        )

let runUpdateCategories (cmd: UpdateCategories) =
    let fileContents = cmd.file |> File.ReadAllText |> GroupCategoryData.Parse

    let keyspace = getKeyspace cmd.keyspace

    let session, queries = buildCassandraSession keyspace

    let existingCategories =
        queries.["fl_readCategories"].Bind()
        |> session.Execute
        |> Seq.map (fun r -> 
            let name = r.["name"] :?> string
            name, { 
                GroupID = r.["group_id"] :?> int
                Name = name
                Words = r.["words"] :?> IDictionary<string, IEnumerable<string>>
            })
        |> Map.ofSeq

    let providedCategories = fileContents.Categories |> Seq.map CategoryEntry.ofFileData |> List.ofSeq

    let oldCategories, newCategories = providedCategories |> List.partition (fun c -> existingCategories |> Map.containsKey c.Name)

    let sameCategories, updatedCategories = oldCategories |> List.partition (fun c -> CategoryEntry.eq c <| existingCategories.[c.Name])

    let providedCategoryNames = providedCategories |> Seq.map (fun c -> c.Name) |> Set.ofSeq
    let categoriesNotProvided = existingCategories |> Map.toSeq |> Seq.map fst |> Seq.filter (fun k -> Set.contains k providedCategoryNames |> not)

    printCategories "New categories" newCategories
    printCategories "Updated categories" updatedCategories
    printCategories "Categories with no changes" sameCategories
    printNames "Categories not in file" categoriesNotProvided

    if
        Seq.isEmpty categoriesNotProvided |> not &&
        promptYesNo "WARNING! Some existing categories were not found in the input file. Continue?" |> not
    then
        raise <| ToolFinished "Cancelled"

    if cmd.dryRun then raise <| ToolFinished "Dry run requested, stopping"

    let message, changeSet =
        if cmd.newOnly then
            sprintf "Will apply %i new categories, continue?" <| List.length newCategories,
            Seq.ofList newCategories
        else if cmd.changesOnly then
            sprintf "Will apply %i changed categories, continue?" <| List.length updatedCategories,
            Seq.ofList updatedCategories
        else if cmd.applyAll then
            sprintf "Will apply all %i categories, continue?" <| List.length providedCategories,
            Seq.ofList providedCategories
        else
            sprintf "Will apply %i new and %i changed categories, continue?" (List.length newCategories) (List.length updatedCategories),
            Seq.append newCategories updatedCategories

    if Seq.isEmpty changeSet then raise <| ToolFinished "No changes to process"

    if not <| promptYesNo message then raise <| ToolFinished "Cancelled"

    applyChangeSet (session, queries) changeSet

    printfn "Done"

    promptAndRunUpdateFromDatabase ()