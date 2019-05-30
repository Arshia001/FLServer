open System
open System.IO

open FSharp.Data
open System.Text

// type OutputJson = JsonProvider<"""..\FLHost\SampleConfig.json""">

type Word = string * string list
type Category = string * Word list
type IncompleteCategory = string option * Word list

let completeCategory (cat: IncompleteCategory) : Category =
    match cat with
    | (Some name, words) -> (name, words)
    | _ -> failwith "Category without name"

let addCategory catList incomplete = 
    completeCategory incomplete :: catList

let joinStrings sep (strs: string seq) = String.Join(sep, strs)

[<EntryPoint>]
let main argv =
    let inputPath = argv.[0]
    let lines = File.ReadAllLines(inputPath)

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

    let finalList = 
        match last with
        | Some x -> addCategory categoryList x
        | _ -> categoryList

    use writer = new StreamWriter(argv.[1], false, Encoding.UTF8)

    // a,"{('aa', {'ad', 'as'})}"
    finalList
        |> Seq.map (fun (name, words) ->
            sprintf "%s,\"{%s}\""
                name
                (
                    words
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
            )
        |> Seq.iter (writer.WriteLine)
    
    0
