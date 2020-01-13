module View

open Fable.React
open Fable.React.Props
open Thoth.Json
open Fulma

open Model

let template content =
    div [ Class "master-container" ] [
        div [ Class "dialog-container" ] [
            div [ Style [ Width "100%" ] ] content
        ]
    ]

let button txt disabled onClick =
    Button.button [
        Button.IsFullWidth
        Button.Color <| if disabled then IsDanger else IsSuccess
        Button.OnClick onClick
        Button.Disabled disabled
        Button.Size Size.IsLarge
    ] [ str txt ]

let label text =
    Field.label [ Field.Label.Size Size.IsMedium; Field.Label.CustomClass "dialog-label" ] [
        str text
    ]

let password labelText error onUpdate =
    let isError, errorText = match error with Some x -> (true, x) | _ -> (false, "")
    Field.div [] [
        label labelText
        Input.password [
            Input.OnChange onUpdate
            if isError then Input.Color IsDanger
        ]
        if isError then Help.help [ Help.Color IsDanger ] [ str errorText ]
    ]

let text t =
    div [ Class "dialog-text" ] [ str t ]

let allowSubmit model =
    model.Password1Error.IsNone && model.Password2Error.IsNone &&
        model.Password1.Length > 0 && model.Password2 = model.Password1

let view (model : Model) (dispatch : Msg -> unit) =
    template <| [
        match model.Status with
        | ValidatingToken ->
            p [] [ text "یه لحظه..." ]

        | TokenValidated true ->
            password "گذرواژه جدید" model.Password1Error (fun e -> dispatch <| Password1Updated e.Value)
            password "تکرار گذرواژه" model.Password2Error (fun e -> dispatch <| Password2Updated e.Value)
            button "تغییر گذرواژه" (allowSubmit model |> not) (fun _ -> dispatch <| UpdatePassword)

        | TokenValidated false ->
            p [] [ text "لینکت اشتباهه یا قبلا منقضی شده. می‌تونی از داخل بازی دوباره درخواست گذرواژه‌ی جدید بدی تا یه لینک جدید برات ارسال شه." ]

        | UpdatingPassword ->
            p [] [ text "یه لحظه..." ]

        | PasswordUpdated (Error e) ->
            Message.message [ Message.Color IsDanger ] [ text <| sprintf "خطایی پیش اومد: %s" e ]

        | PasswordUpdated (Ok ()) ->
            p [] [ text "گذرواژه‌ت به‌روزرسانی شد، حالا می‌تونی با گذرواژه جدیدت وارد بازی بشی." ]
    ]
