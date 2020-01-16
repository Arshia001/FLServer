namespace FLServiceStatus

open System
open System.Threading.Tasks
open FLGrainInterfaces
open Microsoft.Extensions.Hosting
open Orleans
open System.Threading
open Microsoft.Extensions.Logging

type ServiceStatus = Active | Inaccessible | DownForMaintenance

type ClientVersions = { latest: uint32; minimumSupported: uint32 }

type IStatusMonitorService =
    abstract Status : ServiceStatus with get
    abstract SetServiceDownForMaintenance : unit -> unit
    abstract ClearServiceDownForMaintenance : unit -> unit
    abstract SetServiceAccessible : bool -> unit
    abstract SetVersions : ClientVersions -> unit
    abstract Versions : ClientVersions with get

type StatusMonitorService() =
    let mutable downForMaintenance = false
    let mutable accessible = true
    let mutable clientVersion = { latest = 0u; minimumSupported = 0u }

    interface IStatusMonitorService with
        member _.ClearServiceDownForMaintenance() = downForMaintenance <- false
        member _.SetServiceDownForMaintenance() = downForMaintenance <- true
        member _.SetServiceAccessible v = accessible <- v
        member _.Status = if downForMaintenance then DownForMaintenance else if accessible then Active else Inaccessible
        member _.SetVersions v = clientVersion <- v
        member _.Versions = clientVersion

type StatusMonitorHostedService(clusterClient: IClusterClient, statusMonitor: IStatusMonitorService, logger: ILogger<StatusMonitorService>) =
    let cts = new CancellationTokenSource()

    member _.monitor () = async {
        while true do
            try
                let! (latest, minimumSupported) = clusterClient.GetGrain<IServiceStatus>(0L).GetClientVersion() |> Async.AwaitTask
                statusMonitor.SetServiceAccessible true
                statusMonitor.SetVersions { latest = latest; minimumSupported = minimumSupported }
                do! Async.Sleep 5000
            with ex ->
                logger.LogError(ex, "Failed to contact service")
                statusMonitor.SetServiceAccessible(false)
                do! Async.Sleep 1000
    }
        
    interface IHostedService with
        member me.StartAsync(_: Threading.CancellationToken): Threading.Tasks.Task =
            Async.Start(me.monitor(), cts.Token)
            Task.CompletedTask

        member _.StopAsync(_: Threading.CancellationToken): Threading.Tasks.Task = 
            cts.Cancel()
            cts.Dispose()
            Task.CompletedTask
