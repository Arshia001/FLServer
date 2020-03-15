[<AutoOpen>]
module Categories

open FSharp.Data
open System.Collections.Generic

type GroupCategoryData = JsonProvider<"../Misc/category-format.json">

type CategoryEntry = {
    Name: string
    GroupID: int
    Words: IDictionary<string, IEnumerable<string>>
}
with
    static member ofFileData (d: GroupCategoryData.Category) = 
        {
            Name = d.Name
            GroupID = d.GroupId
            Words = 
                let tuples = 
                    d.Words 
                    |> Seq.map (fun w -> w.Word, w.Corrections |> Seq.ofArray)
                System.Linq.Enumerable.ToDictionary(tuples, fst, snd)
        }

    static member eq lhs rhs =
        rhs.Name = lhs.Name &&
        rhs.GroupID = lhs.GroupID &&
        lhs.Words |> Dictionary.equalBy 
            (fun w1 w2 -> 
                Seq.equal w1 w2
            ) rhs.Words 

type GroupEntry = {
    ID: int
    Name: string
}
with
    static member ofFileData (d: GroupCategoryData.Group) =
        {
            ID = d.Id
            Name = d.Name
        }