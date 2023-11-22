namespace Mandadin.Client

open Elmish
open Bolero
open Bolero.Html
open Microsoft.JSInterop
open Bolero.Remoting.Client
open Mandadin.Client.Components
open System

module Main =

  type State =
    { View: View
      Theme: Theme
      CanShare: bool
      HasOverlayControls: bool
      Title: string }

  type Msg =
    | SetView of View
    | TryChangeTheme of Theme
    | ChangeThemeSuccess of bool * Theme
    | GetHasOverlay
    | GetHasOverlaySuccess of bool
    | GetTheme
    | GetThemeSuccess of string
    | CanShare
    | CanShareSuccess of bool
    | Error of exn

  let (|Morning|Evening|Night|Unknown|) =
    function
    | num when num > 4 && num < 12 -> Morning
    | num when num > 11 && num < 20 -> Evening
    | num when num > 19 || num < 5 -> Night
    | num -> Unknown num

  let getGreeting () =
    let hour = DateTime.Now.Hour

    match hour with
    | Morning -> "Buenos dias!"
    | Evening -> "Buenas tardes!"
    | Night -> "Buenas noches!"
    | Unknown num ->
      printfn $"{num}"
      "Hola!"

  let private init (_: 'arg) : State * Cmd<Msg> =
    { View = View.Notes
      Theme = Theme.Dark
      CanShare = false
      HasOverlayControls = false
      Title = getGreeting () },
    Cmd.batch
      [ Cmd.ofMsg GetTheme
        Cmd.ofMsg CanShare
        Cmd.ofMsg GetHasOverlay ]

  let private update
    (msg: Msg)
    (state: State)
    (js: IJSRuntime)
    : State * Cmd<Msg> =
    match msg with
    | SetView view ->
      { state with
          View = view
          Title = getGreeting () },
      Cmd.none
    | TryChangeTheme theme ->
      let jsThemeArg =
        match theme with
        | Theme.Dark -> "Dark"
        | _ -> "Light"

      state,
      Cmd.OfJS.either
        js
        "Mandadin.Theme.SwitchTheme"
        [| jsThemeArg |]
        (fun didChange -> ChangeThemeSuccess(didChange, theme))
        Error
    | ChangeThemeSuccess(didChange, theme) ->
      if didChange then
        { state with Theme = theme }, Cmd.none
      else
        state, Cmd.ofMsg (Error(exn "Failed to change theme"))
    | GetHasOverlay ->
      state,
      Cmd.OfJS.either
        js
        "Mandadin.Theme.HasOverlayControls"
        [||]
        GetHasOverlaySuccess
        Error
    | GetHasOverlaySuccess hasOverlay ->
      { state with
          HasOverlayControls = hasOverlay },
      Cmd.none
    | GetTheme ->
      state,
      Cmd.OfJS.either js "Mandadin.Theme.GetTheme" [||] GetThemeSuccess Error
    | GetThemeSuccess theme ->
      let theme =
        match theme with
        | "Light" -> Theme.Light
        | _ -> Theme.Dark

      let cmd =
        if theme <> state.Theme then
          Cmd.ofMsg (TryChangeTheme theme)
        else
          Cmd.none

      { state with Theme = theme }, cmd
    | CanShare ->
      state,
      Cmd.OfJS.either js "Mandadin.Share.CanShare" [||] CanShareSuccess Error
    | CanShareSuccess canShare -> { state with CanShare = canShare }, Cmd.none
    | Error err ->
      eprintfn "Update Error: [%s]" err.Message
      state, Cmd.none

  let private router =
    Router.infer SetView (fun m -> m.View)


  let private navigateToList (dispatch: Dispatch<Msg>) (route: string) =
    View.ListDetail route |> SetView |> dispatch

  let private goBack (dispatch: Dispatch<Msg>) () =
    SetView View.Lists |> dispatch

  let private view (state: State) (dispatch: Dispatch<Msg>) : Node =
    let getRoute (view: View) = router.HRef view

    let onThemeChangeRequest (theme: Theme) = TryChangeTheme theme |> dispatch

    article {
      attr.``class`` "mandadin"

      cond state.HasOverlayControls
      <| function
        | true -> TitleBar.View(Some state.Title)
        | false -> empty ()

      Navbar.View state.Theme onThemeChangeRequest getRoute

      main {
        attr.``class`` "paper container mandadin-main"

        cond state.View
        <| function
          | View.Import ->
            comp<Views.Import.Page> {
              "OnGoToListRequested" => navigateToList dispatch
            }
          | View.Notes ->
            comp<Views.Notes.Page> { "CanShare" => state.CanShare }
          | View.Lists ->
            comp<Views.Lists.Page> {
              "OnRouteRequested" => navigateToList dispatch
            }
          | View.ListDetail listId ->
            comp<Views.ListItems.Page> {
              "ListId" => ValueSome listId
              "CanShare" => state.CanShare
              "OnBackRequested" => goBack dispatch
            }
      }

      footer {
        attr.``class`` "paper row flex-spaces mandadin-footer"
        p { text "\u00A9 Tunaxor Apps 2020 - 2024" }
        p { text "Mandadin4" }
      }
    }

  type Mandadin() as this =
    inherit ProgramComponent<State, Msg>()

    override _.Program =
      let update msg state = update msg state this.JSRuntime

      Program.mkProgram init update view
      |> Program.withRouter router
#if DEBUG
      |> Program.withConsoleTrace
#endif
