module Api

open Fable.Remoting.Client
open Shared

let api : IRecoveryEmailApi =
  Remoting.createApi()
  |> Remoting.withRouteBuilder Route.builder
  |> Remoting.buildProxy<IRecoveryEmailApi>
