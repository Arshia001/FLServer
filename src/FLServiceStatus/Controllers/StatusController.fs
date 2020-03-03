namespace FLServiceStatus.Controllers

open FLServiceStatus
open Microsoft.AspNetCore.Mvc

type StatusAndVersions = { status: string; latestVersion: uint32; minimumSupportedVersion: uint32 }

[<ApiController>]
[<Route("[controller]")>]
type StatusController (statusMonitor: IStatusMonitorService) =
    inherit ControllerBase()

    [<HttpGet>]
    member _.Get() : StatusAndVersions =
        let status = statusMonitor.Status
        let versions = match status with Active -> statusMonitor.Versions | _ -> { latest = 0u; minimumSupported = 0u }
        {
            status = status |> string
            latestVersion = versions.latest
            minimumSupportedVersion = versions.minimumSupported
        }
