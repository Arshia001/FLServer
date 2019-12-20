module Model

open Shared

type Status = ValidatingToken | TokenValidated of bool | UpdatingPassword | PasswordUpdated of Result<unit, string>

type Route = DefaultRoute of string

type Model = {
    Status: Status
    Token: Token
    Password1: string
    Password1Error: string option
    Password2: string
    Password2Error: string option
}

type Msg =
    | TokenValidationResult of bool
    | Password1Updated of string
    | Password2Updated of string
    | UpdatePassword
    | UpdateComplete of Result<unit, string> // embed validation errors into type system
