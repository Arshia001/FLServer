module Common

open Electron
open Node
open Fable.Core.JsInterop
open Node.Base

type Boolean3 = True3 | False3 | Unknown3

[<RequireQualifiedAccess>]
type SaveResult = Saved | Canceled

[<RequireQualifiedAccess>]
type LoadResult = Loaded of string | Canceled

let writeUtf8Async (text: string) pathAndFilename =
    Promise.create (fun resolve _reject ->
        let write () =
            fs.writeFile(
                pathAndFilename,
                text,
                function
                    | None -> resolve <| Ok ()
                    | Some err -> resolve <| Error err
            )

        fs.exists (
            Fable.Core.U2.Case1 pathAndFilename,
            fun b ->
                if b then
                    fs.unlink (
                        Fable.Core.U2.Case1 pathAndFilename,
                        fun _ -> write ()
                    )
                else
                    write ()
        )
    )

let createDirectory (path: string) =
    promise {
        let! exists = Promise.create (fun resolve _reject ->
            fs.exists(Fable.Core.U2.Case1 path, fun b -> resolve b)
        )

        if (exists) then return Ok ()
        else
            return! Promise.create (fun resolve _reject ->
                fs.mkdir(
                    path,
                    function
                        | None -> resolve <| Ok ()
                        | Some err -> resolve <| Error err
                )
            )
    }

let trimExtension (s: string) = s.Substring(0, s.LastIndexOf('.'))

let executePromises (ps: Lazy<Fable.Core.JS.Promise<Result<unit, 'a>>> seq) =
    if Seq.isEmpty ps then Promise.lift (Ok ())
    else
        (ps |> Seq.reduce (fun p1 p2 -> lazy (p1.Value |> Promise.bind (fun r -> if r.IsError then Promise.lift r else p2.Value)))).Value

let save filterName filterExt (writeFile: string -> Fable.Core.JS.Promise<Result<unit, ErrnoException>>) =
    promise {
        let opts = jsOptions<SaveDialogOptions>(fun o ->
            // See https://github.com/electron/electron/blob/master/docs/api/dialog.md
            o.title <- "Title of save dialog"
            o.defaultPath <- renderer.remote.app.getPath AppPathName.Desktop
            o.filters <-
                [|
                    jsOptions<FileFilter>(fun f ->
                        f.name <- filterName
                        f.extensions <- filterExt
                    )
                |]
        )
        let! res = renderer.remote.dialog.showSaveDialog opts
        if res.canceled then return Ok SaveResult.Canceled
        else
            let! result = writeFile res.filePath
            return result |> Result.map (fun () -> SaveResult.Saved)
    }


let readUtf8Async pathAndFilename =
    Promise.create (fun resolve reject ->
        fs.readFile(
            pathAndFilename,
            "utf8",
            fun optErr contents ->
                match optErr with
                | None -> resolve <| Ok contents
                | Some err -> resolve <| Error err
        )
    )

let load filterName filterExt =
    promise {
        let opts = jsOptions<OpenDialogOptions>(fun o ->
            // See https://github.com/electron/electron/blob/master/docs/api/dialog.md
            o.title <- "Open file..."
            o.defaultPath <- renderer.remote.app.getPath AppPathName.Desktop
            o.filters <-
                [|
                    jsOptions<FileFilter>(fun f ->
                        f.name <- filterName
                        f.extensions <- filterExt
                    )
                |]
        )
        let! res = renderer.remote.dialog.showOpenDialog opts
        if res.canceled then return Ok LoadResult.Canceled
        else
            let! result = readUtf8Async (Seq.head res.filePaths)
            return result |> Result.map LoadResult.Loaded
    }

let readString (s: string) = s.Trim('"').Replace("\\\"", "\"")
