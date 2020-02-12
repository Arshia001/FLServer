module ClusterClientProvider

open System
open System.Threading.Tasks

open Orleans
open Orleans.Configuration
open OrleansCassandraUtils

open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting
open FSharp.Control.Tasks.V2

open SettingsProvider

type IClusterClientProvider =
    abstract ClusterClient : IClusterClient with get

type ClusterClientProvider(client: IClusterClient) =
    interface IClusterClientProvider with
        member _.ClusterClient = client
