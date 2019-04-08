open System
open System.IO

open FSharp.Data

type OutputJson = JsonProvider<"""..\FLHost\SampleConfig.json""">

let addCategory catList (Some name, wordList) = OutputJson.Category(name, wordList |> List.toArray) :: catList

[<EntryPoint>]
let main argv =
    let inputPath = argv.[0]
    let lines = File.ReadAllLines(inputPath)

    let acc = (List.empty<OutputJson.Category>, Option<(string option * OutputJson.Word list)>.None)

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
                    match words |> List.tryFind (fun x -> x.Word = split.[0]) with
                    | Some _ ->
                        Console.WriteLine("Duplicate word " + split.[0])
                        (list, Some (Some name, words))
                    | _ ->
                        let word = OutputJson.Word(split.[0], split |> Array.skip 1 |> Array.filter (fun s -> s <> split.[0]))
                        (list, Some (Some name, word :: words))
                | _, _ -> failwith "Got words before category start"
        ) acc

    let finalList = 
        match last with
        | Some x -> addCategory categoryList x
        | _ -> categoryList

    let output = OutputJson.Root(finalList |> List.toArray)

    use writer = new StreamWriter(File.OpenWrite(argv.[1]))
    output.JsonValue.WriteTo(writer, JsonSaveOptions.None)
    
    0
