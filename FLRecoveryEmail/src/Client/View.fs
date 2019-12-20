module View

open Fable.React
open Fable.React.Props
open Thoth.Json
open Fulma

open Model

let template x =
    x

let button txt disabled onClick =
    Button.button [
        Button.IsFullWidth
        Button.Color <| if disabled then IsDanger else IsSuccess
        Button.Modifiers [ Modifier.TextSize (Screen.All, TextSize.Is1) ]
        Button.OnClick onClick
        Button.Disabled disabled
    ] [ str txt ]

let password label error onUpdate =
    let isError, errorText = match error with Some x -> (true, x) | _ -> (false, "")
    Field.div [] [
        Label.label [] [ str label ]
        Input.password [
            Input.OnChange onUpdate
            if isError then Input.Color IsDanger
        ]
        if isError then Help.help [ Help.Color IsDanger ] [ str errorText ]
    ]

let allowSubmit model =
    model.Password1Error.IsNone && model.Password2Error.IsNone &&
        model.Password1.Length > 0 && model.Password2 = model.Password1

let view (model : Model) (dispatch : Msg -> unit) =
    template <|
    div [] [
        match model.Status with
        | ValidatingToken ->
            p [] [ str "یه لحظه..." ]

        | TokenValidated true ->
            password "گذرواژه جدید" model.Password1Error (fun e -> dispatch <| Password1Updated e.Value)
            password "تکرار گذرواژه" model.Password2Error (fun e -> dispatch <| Password2Updated e.Value)
            button "تغییر گذرواژه" (allowSubmit model |> not) (fun _ -> dispatch <| UpdatePassword)

        | TokenValidated false ->
            p [] [ str "لینکت اشتباهه یا قبلا منقضی شده. می‌تونی از داخل بازی دوباره درخواست گذرواژه‌ی جدید بدی تا یه لینک جدید برات ارسال شه." ]

        | UpdatingPassword ->
            p [] [ str "یه لحظه..." ]

        | PasswordUpdated (Error e) ->
            Message.message [ Message.Color IsDanger ] [ str <| sprintf "خطایی پیش اومد: %s" e ]

        | PasswordUpdated (Ok ()) ->
            Message.message [ Message.Color IsSuccess ] [ str "گذرواژه‌ت به‌روزرسانی شد، حالا می‌تونی با گذرواژه جدیدت وارد بازی بشی." ]
    ]
