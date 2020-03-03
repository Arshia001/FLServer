module Client

open Elmish
open Elmish.React
open Elmish.HMR

open Shared
open Model
open Update
open View

module RouteParser =
    open Elmish.UrlParser

    let routeParser = map DefaultRoute (s "token" </> str)

#if DEBUG
open Elmish.Debug
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.toNavigable (UrlParser.parseHash RouteParser.routeParser) setRoute
|> Program.run
