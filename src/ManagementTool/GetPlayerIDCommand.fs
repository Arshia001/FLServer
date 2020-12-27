[<AutoOpen>]
module GetPlayerIDCommand

open System
open System.IO
open CommandLine
open FLGrainInterfaces
open Orleans

[<Verb("id-from-invite-code", HelpText = "Get a player's ID given their invite code")>]
type GetPlayerID = {
    [<Value(0, MetaName = "Invite code", Required = true)>] inviteCode: string
}

let runGetPlayerID (cmd: GetPlayerID) =
    use client = buildOrleansClient ()
    let player = PlayerIndex.GetByInviteCode(client, cmd.inviteCode.ToUpperInvariant()).Result
    
    if isNull player
    then "Not found"
    else sprintf "%O" (player.GetPrimaryKey())
    |> ToolFinished
    |> raise