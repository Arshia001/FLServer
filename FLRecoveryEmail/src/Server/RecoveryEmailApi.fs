module RecoveryEmailApi

open Shared
open ClusterClientProvider

let mutable exitRequested = false

let createApi (clusterClientProvider: IClusterClientProvider) =

    let client = clusterClientProvider.ClusterClient
    
    {
        initializeWithToken = fun (Token token) -> async { return token.Length > 2 }
        updatePassword = fun (Token token, Password password) -> async { return if password = "123" then Ok () else Error "FU" }
    }
