module AlertDialog

open Fable.React
open Feliz.MaterialUI
open Feliz

type DialogResult = Yes | No | Cancel

type Model<'a> = {
    Open: bool
    Text: string
    YesButtonText: string
    NoButtonText: string option
    CancelButtonText: string option
    ResultCallback: (DialogResult -> 'a) option
}

type Msg<'a> =
    | Open1Button of string * string * (DialogResult -> 'a)
    | Open2Buttons of string * string * string * (DialogResult -> 'a)
    | Open3Buttons of string * string * string * string * (DialogResult -> 'a)
    | Close

let init () = {
    Open = false
    Text = ""
    YesButtonText = ""
    NoButtonText = None
    CancelButtonText = None
    ResultCallback = None
}

let update msg m =
    match msg with
    | Open1Button (text, yesButtonText, f) ->
        if m.Open then m
        else { Open = true; Text = text; YesButtonText = yesButtonText; NoButtonText = None; CancelButtonText = None; ResultCallback = Some f }
    | Open2Buttons (text, yesButtonText, noButtonText, f) ->
        if m.Open then m
        else { Open = true; Text = text; YesButtonText = yesButtonText; NoButtonText = Some noButtonText; CancelButtonText = None; ResultCallback = Some f }
    | Open3Buttons (text, yesButtonText, noButtonText, cancelButtonText, f) ->
        if m.Open then m
        else { Open = true; Text = text; YesButtonText = yesButtonText; NoButtonText = Some noButtonText; CancelButtonText = Some cancelButtonText; ResultCallback = Some f }
    | Close ->
        init ()

let closeWith (model, dispatch, mapDialogResult) result =
    Close |> mapDialogResult |> dispatch
    if model.ResultCallback.IsSome then
        dispatch <| model.ResultCallback.Value result

let AlertDialog<'a> = FunctionComponent.Of((fun(model: Model<'a>, dispatch: 'a -> unit, mapDialogMsg: Msg<'a> -> 'a) ->
    Mui.dialog [
        dialog.open' (model.Open)
        dialog.onClose (fun _ _ -> closeWith (model, dispatch, mapDialogMsg) Cancel)
        dialog.children [
        Mui.dialogContent [
            Mui.dialogContentText model.Text
            ]
        Mui.dialogActions [
            Mui.button [
                prop.onClick (fun _ -> closeWith (model, dispatch, mapDialogMsg) Yes)
                button.color.primary
                button.children model.YesButtonText
            ]
            if model.NoButtonText.IsSome then
                Mui.button [
                    prop.onClick (fun _ -> closeWith (model, dispatch, mapDialogMsg) No)
                    button.color.primary
                    button.children model.NoButtonText.Value
                ]
            if model.CancelButtonText.IsSome then
                Mui.button [
                    prop.onClick (fun _ -> closeWith (model, dispatch, mapDialogMsg) Cancel)
                    button.color.primary
                    button.children model.CancelButtonText.Value
                ]
            ]
        ]
    ]
), "AlertDialog")
