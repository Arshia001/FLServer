namespace FLServiceStatus.Controllers

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open FLServiceStatus
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging

[<ApiController>]
[<Route("[controller]")>]
type StatusController (statusMonitor: IStatusMonitorService) =
    inherit ControllerBase()

    [<HttpGet>]
    member _.Get() : string = statusMonitor.Status.ToString()
