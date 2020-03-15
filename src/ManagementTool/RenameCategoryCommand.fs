[<AutoOpen>]
module RenameCategoryCommand

open CommandLine
open System

[<Verb("rename-category", HelpText = "Rename an existing category")>]
type RenameCategory = {
    [<Option('k', "keyspace", HelpText = "Keyspace name; if left out, will use keyspace specified in mgmtool.keyspace")>] keyspace: string option
    [<Option('o', "old-name", HelpText = "Old category name")>] oldName: string
    [<Option('n', "new-name", HelpText = "New category name")>] newName: string
}

let runRenameCategory (cmd: RenameCategory) =
    let (session, queries) = buildCassandraSession <| getKeyspace cmd.keyspace
    
    let statement = queries.["fl_readCategory"].Bind({| name = cmd.oldName |})
    let rows = session.Execute(statement) |> Seq.toList

    if List.length rows = 0 then raise <| ToolFinished "Category not found"

    let row = List.head rows

    if 
        sprintf "Found category with name '%s', do you wish to rename it to '%s'?"
            (row.["name"] :?> string)
            cmd.newName
        |> promptYesNo
        |> not
    then
        raise <| ToolFinished "Stopped"

    let statement = queries.["fl_readCategory"].Bind({| name = cmd.newName |})
    let newCategoryRows = session.Execute(statement) |> Seq.toList

    let updateNewCategory = 
        if List.length newCategoryRows = 1 then
            promptYesNoCancel <|
                sprintf "Found existing category with name '%s', do you wish to update this category's words with the words of the old category?"
                    ((List.head newCategoryRows).["name"] :?> string)
        else
            true

    // add rename
    let statement = queries.["fl_upsertRenamedCategory"].Bind({| old_name = row.["name"]; new_name = cmd.newName |})
    session.Execute(statement) |> assertEmptyAndIgnore

    // add category with new name
    if updateNewCategory then
        let statement = queries.["fl_upsertCategory"].Bind({| name = cmd.newName; group_id = row.["group_id"]; words = row.["words"] |})
        session.Execute(statement) |> assertEmptyAndIgnore
            
    // delete category with old name
    let statement = queries.["fl_deleteCategory"].Bind({| name = row.["name"] |})
    session.Execute(statement) |> assertEmptyAndIgnore
    
    printfn "Done"
