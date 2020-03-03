module Update

open Elmish
open Shared
open Api
open Model

let setRoute (route: Route option) model =
    match route with
    | Some (DefaultRoute token) ->
        { model with
            Status = ValidatingToken
            Token = Token token
        }, Cmd.OfAsync.either api.initializeWithToken (Token token) TokenValidationResult (fun _ -> TokenValidationResult false)
    | None ->
        { model with Status = TokenValidated false }, Cmd.none

let init route : Model * Cmd<Msg> =
    setRoute route {
        Status = ValidatingToken
        Token = Token ""
        Password1 = ""
        Password1Error = None
        Password2 = ""
        Password2Error = None
    }

let checkPasswordValidity model =
    { model with
        Password1Error = if model.Password1.Length < 3 then Some "گذرواژه‌ت بیش از حد کوتاهه" else None
        Password2Error = if model.Password1 <> model.Password2 && model.Password2.Length > 0 then Some "گذرواژه‌ها یکی نیستن" else None
    }

let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
    match model.Status, msg with
    | ValidatingToken, TokenValidationResult valid ->
        { model with Status = TokenValidated valid }, Cmd.none
    | TokenValidated true, UpdatePassword ->
        { model with Status = UpdatingPassword },
            Cmd.OfAsync.either api.updatePassword (model.Token, Password model.Password1) UpdateComplete
                (sprintf "خطای ارتباط با سرور:‌ %A" >> Error >> UpdateComplete)
    | UpdatingPassword, UpdateComplete r ->
        { model with Status = PasswordUpdated r }, Cmd.none
    | TokenValidated true, Password1Updated password -> checkPasswordValidity { model with Password1 = password }, Cmd.none
    | TokenValidated true, Password2Updated password -> checkPasswordValidity { model with Password2 = password }, Cmd.none
    | _ ->
        printfn "Ignoring update: %A, %A" model.Status msg
        model, Cmd.none
