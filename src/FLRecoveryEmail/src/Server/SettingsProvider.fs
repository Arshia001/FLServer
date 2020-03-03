module SettingsProvider

open FSharp.Data

type SystemSettings = JsonProvider<"system-settings.json">

type ISettingsProvider =
    abstract Settings: SystemSettings.Root with get

type SettingsProvider(settings: SystemSettings.Root) =
    interface ISettingsProvider with
        member _.Settings = settings
