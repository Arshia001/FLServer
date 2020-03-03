module RecoveryEmailApi

open Shared
open ClusterClientProvider
open FSharp.Control.Tasks.V2

open FLGrainInterfaces

let mutable exitRequested = false

let createApi (clusterClientProvider: IClusterClientProvider) =
    let client = clusterClientProvider.ClusterClient
    {
        initializeWithToken = Async.fromTaskF <| fun (Token token) ->
            task {
                let! player = client.GetGrain<IPasswordRecoveryTokenToPlayerConverter>(0L).GetPlayer(token)
                if player = null then return false
                else return! player.ValidatePasswordRecoveryToken(token)
            }
            
        updatePassword = Async.fromTaskF <| fun (Token token, Password password) ->
            task {
                let! player = client.GetGrain<IPasswordRecoveryTokenToPlayerConverter>(0L).GetPlayer(token)
                if player = null then return Error "Invalid token"
                else
                    let! result = player.UpdatePasswordViaRecoveryToken(token, password)
                    match result with
                    | UpdatePasswordViaRecoveryTokenResult.InvalidOrExpiredToken -> return Error "Invalid token"
                    | UpdatePasswordViaRecoveryTokenResult.PasswordNotComplexEnough -> return Error "Password not complex enough"
                    | UpdatePasswordViaRecoveryTokenResult.Success -> return Ok ()
                    | _ -> return Error "Internal error"
            }
    }
