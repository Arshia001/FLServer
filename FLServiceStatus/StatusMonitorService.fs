namespace FLServiceStatus

open System
open System.Threading.Tasks
open FLGrainInterfaces
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.Hosting
open Orleans
open System.Threading
open Microsoft.Extensions.Logging

type ServiceStatus = Active | Inaccessible | DownForMaintenance

type IStatusMonitorService =
    abstract Status : ServiceStatus with get
    abstract SetServiceDownForMaintenance : unit -> unit
    abstract ClearServiceDownForMaintenance : unit -> unit
    abstract SetServiceAccessible : bool -> unit

type StatusMonitorService() =
    let mutable downForMaintenance = false
    let mutable accessible = true

    interface IStatusMonitorService with
        member _.ClearServiceDownForMaintenance() = downForMaintenance <- false
        member _.SetServiceDownForMaintenance() = downForMaintenance <- true
        member _.SetServiceAccessible v = accessible <- v
        member _.Status: ServiceStatus = if downForMaintenance then DownForMaintenance else if accessible then Active else Inaccessible

type StatusMonitorHostedService(clusterClient: IClusterClient, statusMonitor: IStatusMonitorService, logger: ILogger<StatusMonitorService>) =
    let cts = new CancellationTokenSource()

    member _.monitor () = async {
        while true do
            do! Async.Sleep 5000

            try
                let! status = clusterClient.GetGrain<IServiceStatus>(0L).GetStatus() |> Async.AwaitTask
                statusMonitor.SetServiceAccessible(status)
            with ex ->
                logger.LogError(ex, "Failed to contact service")
                statusMonitor.SetServiceAccessible(false)
    }
        
    interface IHostedService with
        member me.StartAsync(_: Threading.CancellationToken): Threading.Tasks.Task =
            Async.Start(me.monitor(), cts.Token)
            Task.CompletedTask

        member _.StopAsync(_: Threading.CancellationToken): Threading.Tasks.Task = 
            cts.Cancel()
            cts.Dispose()
            Task.CompletedTask
