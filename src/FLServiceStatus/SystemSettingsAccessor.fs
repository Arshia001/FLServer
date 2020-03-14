namespace FLServiceStatus

open FSharp.Data

type SystemSettings = JsonProvider<"system-settings.json">

type ISystemSettingsAccessor =
    abstract SystemSettings : SystemSettings.Root with get

type SystemSettingsAccessor(systemSettings: SystemSettings.Root) =
    interface ISystemSettingsAccessor with
        member _.SystemSettings = systemSettings