namespace Shared

[<Struct>] type Token = Token of string
[<Struct>] type Password = Password of string

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

// https://zaid-ajaj.github.io/Fable.Remoting/src/basics.html
type IRecoveryEmailApi = {
    initializeWithToken : Token -> Async<bool>
    updatePassword : Token * Password -> Async<Result<unit, string>>
}
