module WordViewer

open System
open Fable.React
open Feliz
open Feliz.MaterialUI
open Common
open Elmish
open Fable.MaterialUI

type SuggestedWord = {
    Word: string
    Accepted: bool
}

type SuggestedWordsRecord = {
    Owner: Guid
    CategoryName: string
    Words: ResizeArray<SuggestedWord>
    Accepted: Boolean3
}

type Msg =
    | FlipWord of int * int
    | AcceptRecord of int
    | RejectRecord of int
    | SetSelectedRecord of int
    | SetSuggestionGift of int
    | RequestLoad
    | LoadDone of string
    | RequestSave
    | SaveDone
    | ActionFailed of {| ActionName: string; Reason: string |}
    | DialogAction of AlertDialog.Msg<Msg>
    | Nop

type Model = {
    Records: ResizeArray<SuggestedWordsRecord>
    GiftPerSuggestion: int
    DialogModel: AlertDialog.Model<Msg>
    SelectedRecordIndex: int option
}

let read (text: string) =
    let readWords (s: string) = s.Split ',' |> Seq.map (fun w -> { Word = readString w; Accepted = false })

    text.Split '\n'
    |> Seq.choose (fun line ->
        let line = line.Trim '\r'
        if String.IsNullOrEmpty line then None
        else
            let split = line.Split ';'
            if split.Length <> 3 then failwithf "Bad data: %s" line
            Some {
                Owner = Guid.Parse split.[0]
                CategoryName = readString split.[1]
                Words = readWords split.[2] |> ResizeArray
                Accepted = Unknown3
            }
    )
    |> ResizeArray

let writeGifts { Records = records; GiftPerSuggestion = gift } =
    records
    |> Seq.filter (fun r -> r.Accepted = True3)
    |> Seq.map (fun r ->
        let acceptedWords = r.Words |> Seq.filter (fun w -> w.Accepted) |> Seq.map (fun w -> w.Word)
        sprintf "%s,%i,%s,%s"
            (string r.Owner)
            ((acceptedWords |> Seq.length) * gift)
            r.CategoryName
            (String.concat "," acceptedWords)
    )
    |> String.concat "\n"

let writeRecord { Words = words } =
    words
    |> Seq.filter (fun w -> w.Accepted)
    |> Seq.map (fun w -> w.Word)
    |> String.concat "\n"

let writeAll (m: Model) (path: string) =
    let dirName = trimExtension path + " (Update List)"
    executePromises (
        (lazy writeUtf8Async (writeGifts m) path) ::
        (lazy createDirectory dirName) ::
        (
            m.Records
            |> Seq.where (fun r -> r.Accepted = True3)
            |> Seq.map (fun r -> lazy writeUtf8Async (writeRecord r) (dirName + "/" + r.CategoryName + "-" + (string r.Owner) + ".txt"))
            |> Seq.toList
        )
    )

let init () = {
    Records = ResizeArray ()
    GiftPerSuggestion = 10
    DialogModel = AlertDialog.init ()
    SelectedRecordIndex = None
}

let openDialogOneButton text = DialogAction <| AlertDialog.Open1Button (text, "OK", (fun _ -> Nop))

let selectNextRecord m =
    match m.SelectedRecordIndex with
    | Some r -> { m with SelectedRecordIndex = if r < m.Records.Count - 1 then Some (r + 1) else m.SelectedRecordIndex }
    | _ -> m

let update msg m =
    match msg with

    | Nop -> m, Cmd.none

    | FlipWord (r, w) ->
        let word = m.Records.[r].Words.[w]
        m.Records.[r].Words.[w] <- { word with Accepted = not word.Accepted }
        m, Cmd.none

    | AcceptRecord idx ->
        if m.Records.[idx].Words |> Seq.exists (fun w -> w.Accepted) then
            m.Records.[idx] <- { m.Records.[idx] with Accepted = True3 }
            selectNextRecord m, Cmd.none
        else
            m, Cmd.ofMsg (openDialogOneButton "At least one word must be accepted to accept suggestion")

    | RejectRecord idx ->
        if m.Records.[idx].Words |> Seq.exists (fun w -> w.Accepted) |> not then
            m.Records.[idx] <- { m.Records.[idx] with Accepted = False3 }
            selectNextRecord m, Cmd.none
        else
            m, Cmd.ofMsg (openDialogOneButton "Cannot reject suggestion when there is an accepted word")

    | SetSelectedRecord idx -> { m with SelectedRecordIndex = Some idx }, Cmd.none

    | SetSuggestionGift amount -> { m with GiftPerSuggestion = amount }, Cmd.none

    | RequestLoad ->
        m, Cmd.OfPromise.perform (fun () -> load "Word suggestion files" [| "pwsw" |]) ()
            <| function
            | Ok (LoadResult.Loaded txt) -> LoadDone txt
            | Ok LoadResult.Canceled -> Nop
            | Error exn -> ActionFailed {| ActionName = "load"; Reason = exn.message |}

    | LoadDone text -> { m with Records = read text }, Cmd.none

    | RequestSave ->
        if m.Records |> Seq.exists (fun r -> r.Accepted = Unknown3) then
            m, Cmd.ofMsg (openDialogOneButton "Cannot save when some suggestions haven't been accepted or rejected yet")
        else
            let promise =
                save "Word review results" [| "pwwr" |] (writeAll m)
            m, Cmd.OfPromise.perform (fun () -> promise) ()
                <| function
                | Ok SaveResult.Saved -> SaveDone
                | Ok SaveResult.Canceled -> Nop
                | Error exn -> ActionFailed {| ActionName = "Save"; Reason = exn.message |}

    | SaveDone ->
        m, Cmd.ofMsg (openDialogOneButton "Save complete")

    | ActionFailed a ->
        m, Cmd.ofMsg (openDialogOneButton <| sprintf "%s failed due to %s" a.ActionName a.Reason)

    | DialogAction a ->
        { m with DialogModel = AlertDialog.update a m.DialogModel }, Cmd.none


let AppBarCommands model dispatch = [
    Mui.button [
        prop.onClick (fun _ -> dispatch RequestLoad)
        button.color.inherit'
        button.children [
            Mui.typography [
                typography.variant.h6
                typography.color.inherit'
                typography.children "Load"
            ]
        ]
    ]
    if not <| Seq.isEmpty model.Records then
        Mui.button [
            prop.onClick (fun _ -> dispatch RequestSave)
            button.color.inherit'
            button.children [
                Mui.typography [
                    typography.variant.h6
                    typography.color.inherit'
                    typography.children "Save"
                ]
            ]
        ]
]

let recordList model dispatch =
    [
        Mui.list [
            prop.style [
                length.percent 100 |> style.maxHeight
                style.overflow.auto
            ]
            list.children (
                model.Records
                |> Seq.indexed
                |> Seq.map (fun (i, r) ->
                    Mui.listItem [
                        listItem.button true
                        listItem.selected (model.SelectedRecordIndex = Some i)
                        prop.onClick (fun _ -> dispatch <| SetSelectedRecord i)
                        listItem.children [
                            Mui.listItemIcon [
                                listItemIcon.children <|
                                    match r.Accepted with
                                    | True3 -> MaterialDesignIcons.checkIcon []
                                    | False3 -> MaterialDesignIcons.closeIcon []
                                    | Unknown3 -> MaterialDesignIcons.helpIcon []
                            ]
                            Mui.listItemText [
                                listItemText.children r.CategoryName
                            ]
                        ]
                    ]
                )
            )
        ]
    ]

let recordViewer model dispatch =
    match model.SelectedRecordIndex with
    | Some i when i >= 0 && i < model.Records.Count ->
        [
            Html.div [
                prop.style [
                    style.display.flex
                    style.flexDirection.column
                    length.percent 100 |> style.height
                ]
                prop.children [
                    Mui.list [
                        prop.style [
                            Feliz.Interop.mkStyle "max-height" "calc(100% - 72px)"
                            Feliz.Interop.mkStyle "height" "calc(100% - 72px)"
                            style.overflow.auto
                        ]
                        list.children (
                            model.Records.[i].Words
                            |> Seq.indexed
                            |> Seq.map (fun (wi, w) ->
                                Mui.listItem [
                                    listItem.button true
                                    prop.onClick (fun _ -> dispatch <| FlipWord (i, wi))
                                    listItem.children [
                                        Mui.listItemIcon [
                                            listItemIcon.children <|
                                                match w.Accepted with
                                                | true -> MaterialDesignIcons.checkIcon []
                                                | false -> MaterialDesignIcons.closeIcon []
                                        ]
                                        Mui.listItemText [
                                            listItemText.children w.Word
                                        ]
                                    ]
                                ]
                            )
                        )
                    ]
                    Html.div [
                        prop.style [
                            style.flexGrow 0
                            style.alignItems.center
                            style.height 72
                            style.margin 12
                            style.display.flex
                        ]
                        prop.children [
                            Mui.textField [
                                textField.variant.outlined
                                textField.label "Reward per suggestion"
                                textField.value model.GiftPerSuggestion
                                textField.type' "number"
                                textField.onChange (fun s ->
                                    match s, Int32.TryParse s with
                                    | _, (true, i) -> dispatch <| SetSuggestionGift i
                                    | "", _ -> dispatch <| SetSuggestionGift 0
                                    | _ -> ()
                                )
                            ]
                            Html.div [
                                prop.style [
                                    style.flexGrow 1
                                ]
                            ]
                            Mui.button [
                                button.children "Reject"
                                button.color.secondary
                                prop.onClick (fun _ -> dispatch <| RejectRecord i)
                            ]
                            Mui.button [
                                button.children "Accept"
                                button.color.primary
                                prop.onClick (fun _ -> dispatch <| AcceptRecord i)
                            ]
                        ]
                    ]
                ]
            ]
        ]
    | _ -> []

let private useStyles = Styles.makeStyles(fun styles _theme ->
    {|
        root = styles.create [
            style.height (length.percent 100)
        ]
        toolbar = styles.create [
            style.padding 8
            style.flexGrow 0
        ]
        grid = styles.create [
            Feliz.Interop.mkStyle "height" "calc(100% + 8px)" // calc missing from Feliz?
        ]
        panelGridCell = styles.create [
            style.height (length.percent 100)
        ]
        panel = styles.create [
            style.height (length.percent 100)
        ]
    |}
)

let WordViewerPage : FunctionComponent<Model * (Msg -> unit)> = FunctionComponent.Of((fun (model, dispatch) ->
    let c = useStyles ()
    Html.div [
        prop.className c.root
        prop.children [
            Mui.grid [
                grid.container true
                grid.spacing._1
                prop.className c.grid
                grid.children [
                    Mui.grid [
                        grid.xs._4
                        grid.item true
                        prop.className c.panelGridCell
                        grid.children [
                            Mui.paper [
                                prop.className c.panel
                                prop.children (recordList model dispatch)
                            ]
                        ]
                    ]
                    Mui.grid [
                        grid.xs._8
                        grid.item true
                        prop.className c.panelGridCell
                        grid.children [
                            Mui.paper [
                                prop.className c.panel
                                prop.children (recordViewer model dispatch)
                            ]
                        ]
                    ]
                ]
            ]
            AlertDialog.AlertDialog (model.DialogModel, dispatch, DialogAction)
        ]
    ]
), "WordViewerPage")
