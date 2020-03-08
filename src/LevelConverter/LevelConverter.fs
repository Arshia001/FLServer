open System
open System.IO

open FSharp.Data
open System.Text
open Microsoft.FSharp.Core.Printf

// type OutputJson = JsonProvider<"""..\FLHost\SampleConfig.json""">

type Word = string * string list
type Category = string * Word list
type Group = int * string * Category list
type IncompleteCategory = string option * Word list

let completeCategory (cat: IncompleteCategory) : Category =
    match cat with
    | (Some name, words) -> (name, words)
    | _ -> failwith "Category without name"

let addCategory catList incomplete = 
    completeCategory incomplete :: catList

let joinStrings sep (strs: string seq) = String.Join(sep, strs)

let readCsv path =
    let lines = File.ReadAllLines(path)

    let empty = (List.empty<Category>, Option<IncompleteCategory>.None)

    let (categoryList, last) = 
        lines |>
        Array.fold (fun (list, last) line -> 
            if line.StartsWith('#') then
                match last with
                | Some x -> (addCategory list x, Some (None, List.empty))
                | _ -> (list, Some (None, List.empty))
            else
                let split = line.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries)
                match (split, last) with
                | [||], _ -> (list, last)
                | _, Some (None, words) -> (list, Some (Some split.[0], words))
                | _, Some (Some name, words) -> 
                    match words |> List.tryFind (fun (name, _) -> name = split.[0]) with
                    | Some _ ->
                        Console.WriteLine("Duplicate word " + split.[0])
                        (list, last)
                    | _ ->
                        let word = (split.[0], split |> Seq.skip 1 |> Seq.filter (fun s -> s <> split.[0]) |> List.ofSeq)
                        (list, Some (Some name, word :: words))
                | _, None -> failwith "Got words before category start"
        ) empty

    match last with
    | Some x -> addCategory categoryList x
    | _ -> categoryList

let writeCategory (writer: StreamWriter) id (name, words) =
    // a,1,"{('aa', {'ad', 'as'})}"
    sprintf "%s,%i,\"{%s}\""
        name
        id
        (words 
            |> Seq.map (fun (main, corrections) -> 
                sprintf "'%s': {%s}"
                    main
                    (corrections
                        |> Seq.map (sprintf "'%s'")
                        |> joinStrings ", "
                    )
            )
            |> joinStrings ", "
        )
        |> writer.WriteLine

let csvToCassandra inputPath outputPath =
    use writer = new StreamWriter(outputPath, false, Encoding.UTF8)
    readCsv inputPath |> Seq.iter (writeCategory writer 0)
    
let csvToFolder inputPath outputPath =
    let categories = readCsv inputPath
    Directory.CreateDirectory(outputPath) |> ignore
    categories |> Seq.iter (fun (name, words) ->
        use writer = new StreamWriter(Path.Combine(outputPath, name + ".txt"), false, Encoding.UTF8)
        words |> Seq.iter (fun (main, corrections) ->
            if corrections |> List.isEmpty then
                sprintf "%s" main
            else
                sprintf "%s:%s" main (corrections |> joinStrings "|")
            |> writer.WriteLine
        )
    )

let readCategoryWord (line: string) : Word option =
    let parts = line.Split(':', 2)
    match parts with
    | [| "" |] -> None
    | [| name |] -> (name, []) |> Some
    | [| _; corrections |] when corrections.IndexOf(':') >= 0 -> failwithf "Corrections include ':' character, but should be separated with '|' : %s" line
    | [| name; corrections |] -> (name, corrections.Split('|') |> List.ofArray) |> Some
    | _ -> failwithf "Invalid word definition %s" line

let readCategory (path: string) : Category =
    try
        let name = Path.GetFileNameWithoutExtension(path)
        let words =
            File.ReadAllLines(path)
            |> Seq.choose readCategoryWord
            |> List.ofSeq

        if List.isEmpty words then failwithf "Empty category: %s" name
        
        let duplicateWords =
            words
            |> Seq.map fst
            |> Seq.append (words |> Seq.map snd |> Seq.concat)
            |> Seq.groupBy id
            |> Seq.filter (fun (_, items) -> Seq.length items > 1)
            |> Seq.toList
        if List.isEmpty duplicateWords |> not then 
            duplicateWords
            |> Seq.map fst
            |> String.concat ", "
            |> failwithf "Non-unique words found in category: %s"

        (name, words)
    with ex -> Exception(sprintf "Failure while reading file %s" path, ex) |> raise

let readGroup (directory: string) : Group =
    let dirName = Path.GetFileName(directory)
    match dirName.Split([| '-' |], 2) with
    | [| idString; name |] ->
        let id = int idString
        (id, name, Directory.GetFiles(directory) |> Seq.map readCategory |> List.ofSeq)
    | _ -> 
        failwithf "Invalid directory name: %s" dirName

let folderToCassandra inputPath outputPath =
    if Directory.GetFiles(inputPath).Length > 0 then
        printfn "There cannot be any files in the root of the groups directory"
        Environment.Exit -1
    else
        Directory.CreateDirectory(outputPath) |> ignore
        use groupsWriter = new StreamWriter(Path.Combine(outputPath, "groups.csv"))
        use categoriesWriter = new StreamWriter(Path.Combine(outputPath, "categories.csv"))

        Directory.GetDirectories inputPath
        |> Seq.map readGroup
        |> Seq.iter (fun (id, name, words) ->
            sprintf "%i,%s" id name |> groupsWriter.WriteLine
            words |> Seq.iter (writeCategory categoriesWriter id)
        )

[<EntryPoint>]
let main argv =
    let command = argv.[0]
    let inputPath = argv.[1]
    let outputPath = argv.[2]
    match command with
    | "csvToCassandra" -> csvToCassandra inputPath outputPath
    | "csvToFolder" -> csvToFolder inputPath outputPath
    | "folderToCassandra" -> folderToCassandra inputPath outputPath
    | _ -> printfn "Unknown command, should be one of csvToCassandra, csvToFolder, folderToCassandra"
    0