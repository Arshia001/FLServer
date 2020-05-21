module CategoryViewer

open System
open Fable.React
open Feliz
open Feliz.MaterialUI
open Common
open Elmish
open Fable.MaterialUI
open Fable.Core

type SuggestedCategoryRecord = {
    Owner: Guid
    CategoryName: string
    Words: string
    WordCount: int
    Accepted: Boolean3
}

type Msg =
    | SetWordCount of {| categoryIndex: int; count: int |}
    | AcceptRecord of int
    | RejectRecord of int
    | SetSelectedRecord of int
    | SetSuggestionGiftPerWord of int
    | SetSuggestionGiftPerCategory of int
    | RequestLoad
    | LoadDone of string
    | RequestSave
    | SaveDone
    | ActionFailed of {| ActionName: string; Reason: string |}
    | DialogAction of AlertDialog.Msg<Msg>
    | Nop

type Model = {
    Records: ResizeArray<SuggestedCategoryRecord>
    GiftPerWord: int
    GiftPerCategory: int
    DialogModel: AlertDialog.Model<Msg>
    SelectedRecordIndex: int option
}

let read (text: string) =
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
                Words = readString split.[2]
                WordCount = 0
                Accepted = Unknown3
            }
    )
    |> ResizeArray

let writeGifts { Records = records; GiftPerWord = wordGift; GiftPerCategory = catGift } =
    records
    |> Seq.filter (fun r -> r.Accepted = True3)
    |> Seq.map (fun r ->
        sprintf "%s,%i,%s"
            (string r.Owner)
            (r.WordCount * wordGift + catGift)
            r.CategoryName
    )
    |> String.concat "\n"

let writeAll (m: Model) (path: string) =
    let dirName = trimExtension path + " (Category List)"
    Promise.sequential (
        (lazy writeUtf8Async (writeGifts m) path) ::
        (lazy createDirectory dirName) ::
        (
            m.Records
            |> Seq.where (fun r -> r.Accepted = True3)
            |> Seq.map (fun r -> lazy writeUtf8Async r.Words (dirName + "/" + r.CategoryName + " - " + r.Owner.ToString() + ".txt"))
            |> Seq.toList
        )
    )

let init () = {
    Records = ResizeArray ()
    GiftPerWord = 10
    GiftPerCategory = 100
    DialogModel = AlertDialog.init ()
    SelectedRecordIndex = None
}

let openDialogOneButton text = DialogAction <| AlertDialog.Open1Button (text, "OK", (constOf Nop))

let selectNextRecord m =
    match m.SelectedRecordIndex with
    | Some r -> { m with SelectedRecordIndex = if r < m.Records.Count - 1 then Some (r + 1) else m.SelectedRecordIndex }
    | _ -> m

let update msg m =
    match msg with

    | Nop -> m, Cmd.none

    | SetWordCount w ->
        let record = m.Records.[w.categoryIndex]
        m.Records.[w.categoryIndex] <- { record with WordCount = w.count }
        m, Cmd.none

    | AcceptRecord idx ->
        if m.Records.[idx].WordCount > 0 then
            m.Records.[idx] <- { m.Records.[idx] with Accepted = True3 }
            selectNextRecord m, Cmd.none
        else
            m, Cmd.ofMsg (openDialogOneButton "Word count must be entered and be greater than zero before accepting a suggestion")

    | RejectRecord idx ->
        m.Records.[idx] <- { m.Records.[idx] with Accepted = False3 }
        selectNextRecord m, Cmd.none

    | SetSelectedRecord idx -> { m with SelectedRecordIndex = Some idx }, Cmd.none

    | SetSuggestionGiftPerWord amount -> { m with GiftPerWord = amount }, Cmd.none

    | SetSuggestionGiftPerCategory amount -> { m with GiftPerCategory = amount }, Cmd.none

    | RequestLoad ->
        m, Cmd.OfPromise.perform (fun () -> load "Category suggestion files" [| "pwsc" |]) ()
            <| function
            | Ok (LoadResult.Loaded txt) -> LoadDone txt
            | Ok LoadResult.Canceled -> Nop
            | Error exn -> ActionFailed {| ActionName = "load"; Reason = exn.message |}

    | LoadDone text -> { m with Records = read text }, Cmd.none

    | RequestSave ->
        if m.Records |> Seq.exists (fun r -> r.Accepted = Unknown3) then
            m, Cmd.ofMsg (openDialogOneButton "Cannot save when some suggestions haven't been accepted or rejected yet")
        else
            let promise = save "Category review results" [| "pwcr" |] (writeAll m)
            m, Cmd.OfPromise.perform (fun () -> promise) ()
                <| function
                | Ok (SaveResult.Saved ()) -> SaveDone
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
                    Html.pre [
                        prop.style [
                            Feliz.Interop.mkStyle "max-height" "calc(100% - 72px)"
                            Feliz.Interop.mkStyle "height" "calc(100% - 72px)"
                            style.padding 10
                            style.fontFamily "inherit"
                            style.overflow.auto
                        ]
                        prop.text (model.Records.[i].Words)
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
                                prop.style [
                                    style.width 150
                                ]
                                textField.variant.outlined
                                textField.label "WORD COUNT"
                                textField.value model.Records.[i].WordCount
                                textField.type' "number"
                                textField.onChange (fun s ->
                                    match s, Int32.TryParse s with
                                    | _, (true, cnt) -> dispatch <| SetWordCount {| categoryIndex = i; count = cnt |}
                                    | "", _ -> dispatch <| SetWordCount {| categoryIndex = i; count = 0 |}
                                    | _ -> ()
                                )
                            ]
                            Mui.textField [
                                prop.style [
                                    style.width 100
                                    style.marginLeft 10
                                ]
                                textField.variant.outlined
                                textField.label "Reward per word"
                                textField.value model.GiftPerWord
                                textField.type' "number"
                                textField.onChange (fun s ->
                                    match s, Int32.TryParse s with
                                    | _, (true, i) -> dispatch <| SetSuggestionGiftPerWord i
                                    | "", _ -> dispatch <| SetSuggestionGiftPerWord 0
                                    | _ -> ()
                                )
                            ]
                            Mui.textField [
                                prop.style [
                                    style.width 100
                                    style.marginLeft 10
                                ]
                                textField.variant.outlined
                                textField.label "Reward per category"
                                textField.value model.GiftPerCategory
                                textField.type' "number"
                                textField.onChange (fun s ->
                                    match s, Int32.TryParse s with
                                    | _, (true, i) -> dispatch <| SetSuggestionGiftPerCategory i
                                    | "", _ -> dispatch <| SetSuggestionGiftPerCategory 0
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

let CategoryViewerPage : FunctionComponent<Model * (Msg -> unit)> = FunctionComponent.Of((fun (model, dispatch) ->
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
), "CategoryViewerPage")
