[<AutoOpen>]
module ReadSuggestionsCommand

open System
open System.IO
open CommandLine

[<Verb("read-suggestions", HelpText = "Read suggested words and categories from database and export to file")>]
type ReadSuggestions = {
    [<Option('k', "keyspace", HelpText = "Keyspace name; if left out, will use keyspace specified in mgmtool.keyspace")>] keyspace: string option
    [<Option('c', "category", HelpText = "Read categories", SetName = "category", Required = true)>] category: bool
    [<Option('w', "word", HelpText = "Read words", SetName = "word", Required = true)>] word: bool
    [<Option('f', "output-file", HelpText = "Output file path", Required = true)>] fileName: string
}

let normalizeString (s: string) = s.Replace("\"", "\\\"").Replace("\r\n", "\\n").Replace("\n", "\\n")

let writeString (writer: StreamWriter) = normalizeString >> sprintf "\"%s\"" >> writer.Write

let enforceFileExtension (ext: string) (path: string) =
    let extWithDot = if ext.StartsWith '.' then ext else sprintf ".%s" ext
    if path.EndsWith extWithDot then path else path + extWithDot

let ensureFileIsMissing path =
    if File.Exists path then
        path |> sprintf "File already exists: %s" |> ToolFinished |> raise
    else
        path

let runReadSuggestions (cmd: ReadSuggestions) =
    let keyspace = getKeyspace cmd.keyspace
    let (session, queries) = buildCassandraSession keyspace

    let shouldDelete = promptYesNo "Delete data from database after export?"

    if cmd.category then
        let path = enforceFileExtension "pwsc" cmd.fileName |> ensureFileIsMissing
        use writer = new StreamWriter(path, false)

        let categories = 
            queries.["fl_ReadSuggestedCategories"].Bind()
            |> session.Execute
            |> Seq.map (fun row -> 
                {|
                    ownerID = row.["owner_id"] :?> Guid
                    name = row.["name"] :?> string
                    words = row.["words"] :?> string
                |}
                )
            |> Seq.toList

        categories
            |> Seq.iter (fun c ->
                writer.Write c.ownerID
                writer.Write ';'
                writeString writer c.name
                writer.Write ';'
                writeString writer c.words
                writer.WriteLine()
                )
        
        if shouldDelete then
            categories
            |> Seq.iter (fun c ->
                queries.["fl_DeleteSuggestedCategory"].Bind({| owner_id = c.ownerID; name = c.name |})
                    |> executeNonQuery session
                )

    else if cmd.word then
        let path = enforceFileExtension "pwsw" cmd.fileName |> ensureFileIsMissing
        use writer = new StreamWriter(path, false)

        let words = 
            queries.["fl_ReadSuggestedWords"].Bind()
            |> session.Execute
            |> Seq.map (fun row -> 
                {|
                    ownerID = row.["owner_id"] :?> Guid
                    categoryName = row.["category_name"] :?> string
                    words = row.["words"] :?> string seq
                |}
                )
            |> Seq.toList

        words
            |> Seq.iter (fun c ->
                writer.Write c.ownerID
                writer.Write ';'
                writeString writer c.categoryName
                writer.Write ';'
                c.words |> Seq.iterInterleaved (fun () -> writer.Write(",")) (writeString writer)
                writer.WriteLine()
                )
        
        if shouldDelete then
            words
            |> Seq.iter (fun c ->
                queries.["fl_DeleteSuggestedWord"].Bind({| owner_id = c.ownerID; category_name = c.categoryName |})
                    |> executeNonQuery session
                )
    else
        ToolFinished "Either word or category must be specified" |> raise
    
    ToolFinished "Done" |> raise