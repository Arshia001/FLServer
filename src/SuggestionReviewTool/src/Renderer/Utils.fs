[<AutoOpen>]
module Utils

open Fable.Core
open Browser.Types

[<Global>]
let private __static : string = jsNative

/// Prefixes the string with the static asset root path.
let getStatic (pathInStaticDir: string) =
  #if DEBUG
  pathInStaticDir
  #else
  __static + "/" + pathInStaticDir
  #endif

let preventDefault (e: Event) =
  e.preventDefault ()

/// Returns the input if Some, otherwise returns the empty string. Use this to
/// indicate "no value" in controlled `value` props that are not allowed to
/// have `null`, e.g. `select.value`. This is similar to `Option.defaultValue
/// ""` except that it works for any inner type.
let emptyStringIfNone x =
  if x |> unbox<obj> |> isNull then "" |> unbox<'a option> else x


[<AutoOpen>]
module Extensions =

  type Result<'T,'TError> with

    member this.IsOk =
      match this with Ok _ -> true | Error _ -> false

    member this.IsError =
      match this with Error _ -> true | Ok _ -> false

    member this.ErrorOr value =
      match this with Ok _ -> value | Error err -> err



module String =

  let ensureEndsWith suffix (str: string) =
    if str.EndsWith suffix then str else str + suffix
