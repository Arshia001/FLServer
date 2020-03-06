#r "paket: groupref build //"
#load "./.fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

// Constants

let buildConfig = "Release"
let publishFramework = "netcoreapp3.1"

// End of constants

open System

open Fake.Core
open Fake.DotNet
open Fake.IO

Target.initEnvironment ()

let hostPath = Path.getFullName "./src/FLHost"
let serviceStatusPath = Path.getFullName "./src/FLServiceStatus"
let passwordRecoveryPath = Path.getFullName "./src/FLRecoveryEmail"
let networkMessagesPath = Path.getFullName "./src/FLNetworkMessages"

let publishDir = Path.getFullName "./publish"

let ensureTool tool = 
    match ProcessUtils.tryFindFileOnPath tool with
    | Some t -> t
    | _ -> failwith <| tool + " was not found in path"

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let runFakeTarget cmd = runDotNet <| "fake build -t " + cmd

let cmd = ensureTool "cmd"

Target.create "Clean" <|
    fun _ ->
        Shell.cleanDir publishDir
        runFakeTarget "Clean" passwordRecoveryPath

Target.create "BuildMessages" <| fun _ -> runTool cmd "/c RunLMCodeGen.bat" networkMessagesPath

Target.create "BuildHost" <| fun _ -> runDotNet ("build -c " + buildConfig) hostPath

Target.create "BuildServiceStatus" <| fun _ -> runDotNet ("build -c " + buildConfig) serviceStatusPath

Target.create "BuildPasswordRecovery" <| fun _ -> runFakeTarget "build" passwordRecoveryPath

Target.create "Build" <| 
    fun _ ->
        Target.run 1 "BuildHost" []
        Target.run 1 "BuildServiceStatus" []
        Target.run 1 "BuildPasswordRecovery" []

Target.create "RunHost" <| fun _ -> runDotNet "run" hostPath

Target.create "RunServiceStatus" <| fun _ -> runDotNet "run" serviceStatusPath

Target.create "RunPasswordRecovery" <| fun _ -> runFakeTarget "run" passwordRecoveryPath

Target.create "Run" <|
    fun _ ->
        [
            async { Target.run 1 "RunHost" [] }
            async { Target.run 1 "RunServiceStatus" [] }
            async { Target.run 1 "RunPasswordRecovery" [] }
        ] |> Async.Parallel |> Async.Ignore |> Async.RunSynchronously

let runDotNetPublish platform path outputPath =
    let args = sprintf "publish -o \"%s\" -c %s -r %s --no-self-contained -f %s" outputPath buildConfig platform publishFramework
    runDotNet args path

Target.create "Publish" <|
    fun p ->
        let platform =
            p.Context.Arguments
            |> List.tryHead
            |> Option.defaultWith (fun () ->
                Trace.logToConsole ("No platform specified, will use linux-x64", Trace.Warning)
                "linux-x64"
            )

        let hostOutPath = Path.combine publishDir "flserver"
        let serviceStatusOutPath = Path.combine publishDir "flservicestatus"
        let passwordRecoveryOutPath = Path.combine publishDir "flpasswordrecovery"
         
        [
            async { runDotNetPublish platform hostPath hostOutPath }
            async { runDotNetPublish platform serviceStatusPath serviceStatusOutPath }
            async {
                let args = sprintf "bundle %s %s %s \"%s\"" buildConfig platform publishFramework passwordRecoveryOutPath
                runFakeTarget args passwordRecoveryPath
            }
        ] |> Async.Parallel |> Async.Ignore |> Async.RunSynchronously
        

open Fake.Core.TargetOperators

"Clean"
    ==> "BuildMessages"
    ==> "BuildHost"
    ==> "RunHost"

"BuildHost"
    ==> "BuildServiceStatus"
    ==> "RunServiceStatus"

"BuildHost"
    ==> "BuildPasswordRecovery"
    ==> "RunPasswordRecovery"

"Build"
    ==> "Run"

"Build"
    ==> "Publish"

Target.runOrDefaultWithArguments "Build"
