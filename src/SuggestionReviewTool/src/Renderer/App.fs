module App

open Electron
open Elmish
open Elmish.React
open Fable.Core.JsInterop
open Fable.React
open Feliz
open Feliz.MaterialUI
open Fable.MaterialUI.Icons
open Fable.MaterialUI.MaterialDesignIcons


type Page =
  | Home
  | WordViewer
  | CategoryViewer
  static member All =
    [ WordViewer; CategoryViewer ]

let pageTitle = function
  | Home -> "Home"
  | WordViewer -> "Review suggested words"
  | CategoryViewer -> "Review suggested categories"

type ThemeMode =
  | Light
  | Dark

type Msg =
  | Navigate of Page
  | SetSystemThemeMode of ThemeMode
  | WordViewerMsg of WordViewer.Msg
  | CategoryViewerMsg of CategoryViewer.Msg

type Model =
  { Page: Page
    SystemThemeMode: ThemeMode
    WordViewer: WordViewer.Model
    CategoryViewer: CategoryViewer.Model }

let resetPageStates m =
  { m with
      WordViewer = WordViewer.init ()
      CategoryViewer = CategoryViewer.init () }

let update msg m =
  match msg with
  | Navigate p ->
      if m.Page = p then m, Cmd.none
      else
        { m with Page = p } |> resetPageStates, Cmd.none
  | SetSystemThemeMode mode ->
      { m with SystemThemeMode = mode }, Cmd.none
  | WordViewerMsg msg' ->
      let m', cmd = WordViewer.update msg' m.WordViewer
      { m with WordViewer = m' }, Cmd.map WordViewerMsg cmd
  | CategoryViewerMsg msg' ->
      let m', cmd = CategoryViewer.update msg' m.CategoryViewer
      { m with CategoryViewer = m' }, Cmd.map CategoryViewerMsg cmd

module Theme =
  let light = Styles.createMuiTheme([
    theme.palette.type'.light
    theme.palette.primary Colors.indigo
    theme.palette.secondary Colors.pink
  ])

  let dark = Styles.createMuiTheme([
    theme.palette.type'.dark
    theme.palette.primary Colors.lightBlue
    theme.palette.secondary Colors.pink
    theme.props.muiAppBar [
      appBar.color.default'
    ]
  ])

let private pageListItem model dispatch page =
  Mui.listItem [
    prop.key (pageTitle page)
    prop.onClick (fun _ -> Navigate page |> dispatch)
    listItem.button true
    listItem.divider ((page = Home))
    listItem.selected (model.Page = page)
    listItem.children [
      Mui.listItemText (pageTitle page)
    ]
  ]

let private pageView model dispatch =
  match model.Page with
  | Home -> Mui.typography "Choose desired functionality from side menu."
  | WordViewer -> WordViewer.WordViewerPage (model.WordViewer, WordViewerMsg >> dispatch)
  | CategoryViewer -> CategoryViewer.CategoryViewerPage (model.CategoryViewer, CategoryViewerMsg >> dispatch)


let private useToolbarTyles = Styles.makeStyles(fun styles theme ->
  {|
    appBarTitle = styles.create [
      style.flexGrow 1
    ]
  |}
)

let private getPageCommands model dispatch =
    match model.Page with
    | WordViewer -> WordViewer.AppBarCommands model.WordViewer (WordViewerMsg >> dispatch) |> List.toSeq
    | CategoryViewer -> CategoryViewer.AppBarCommands model.CategoryViewer (CategoryViewerMsg >> dispatch) |> List.toSeq
    | _ -> Seq.empty

let Toolbar = FunctionComponent.Of((fun (model, dispatch) ->
  let c = useToolbarTyles ()
  Mui.toolbar [
    Mui.typography [
      typography.variant.h6
      typography.color.inherit'
      typography.children (pageTitle model.Page)
      typography.classes.root c.appBarTitle
    ]
    yield! getPageCommands model dispatch
  ]
), "Toolbar", memoEqualsButFunctions)


let private useRootViewStyles = Styles.makeStyles(fun styles theme ->
  let drawerWidth = 240
  {|
    root = styles.create [
      style.display.flex
      style.userSelect.none
      style.height (length.percent 100)
    ]
    appBar = styles.create [
      style.zIndex (theme.zIndex.drawer + 1)
    ]
    appBarTitle = styles.create [
      style.flexGrow 1
    ]
    drawer = styles.create [
      style.width (length.px drawerWidth)
      style.flexShrink 0
    ]
    drawerPaper = styles.create [
      style.width (length.px drawerWidth)
    ]
    content = styles.create [
      style.display.flex
      style.flexDirection.column
      style.flexGrow 1
      style.padding (theme.spacing 3)
      Feliz.Interop.mkStyle "max-height" "calc(100% - 64px)"
    ]
    toolbar = styles.create [
      yield! theme.mixins.toolbar
    ]
  |}
)

let RootView = FunctionComponent.Of((fun (model, dispatch) ->
  let c = useRootViewStyles ()
  Mui.themeProvider [
    themeProvider.theme (
      match model.SystemThemeMode with
      | Dark -> Theme.dark
      | Light -> Theme.light
    )
    themeProvider.children [
      Html.div [
        prop.className c.root
        prop.children [
          Mui.cssBaseline []
          Mui.appBar [
            appBar.classes.root c.appBar
            appBar.position.fixed'
            appBar.children [
              Toolbar(model, dispatch)
            ]
          ]
          Mui.drawer [
            drawer.variant.permanent
            drawer.classes.root c.drawer
            drawer.classes.paper c.drawerPaper
            drawer.children [
              Html.div [ prop.className c.toolbar ]
              Mui.list [
                list.component' "nav"
                list.children (Page.All |> List.map (pageListItem model dispatch) |> ofList)
              ]
            ]
          ]
          Html.main [
            prop.className c.content
            prop.children [
              Html.div [ prop.className c.toolbar ]
              pageView model dispatch
            ]
          ]
        ]
      ]
    ]
  ]
), "RootView", memoEqualsButFunctions)

let updateSystemTheme dispatch =
  let dispatchCurrentMode () =
    if renderer.remote.nativeTheme.shouldUseDarkColors
    then dispatch (SetSystemThemeMode Dark)
    else dispatch (SetSystemThemeMode Light)
  renderer.remote.nativeTheme.onUpdated(fun _ -> dispatchCurrentMode ()) |> ignore
  dispatchCurrentMode ()

let init () =
  let m =
    { Page = Home
      SystemThemeMode = Light
      WordViewer = WordViewer.init ()
      CategoryViewer = CategoryViewer.init () }
  m, Cmd.ofSub updateSystemTheme

let view model dispatch =
  RootView (model, dispatch)
