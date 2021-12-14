namespace Mandadin

open Elmish
open Bolero
open Bolero.Html
open Microsoft.JSInterop
open Bolero.Remoting.Client
open Mandadin.Components
open System

module Main =

  type State =
    { View: View
      CanShare: bool
      HasOverlayControls: bool
      Title: string }

  type Msg =
    | SetView of View
    | GetHasOverlay
    | GetHasOverlaySuccess of bool
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
      CanShare = false
      HasOverlayControls = false
      Title = getGreeting () },
    Cmd.batch [
      Cmd.ofMsg CanShare
      Cmd.ofMsg GetHasOverlay
    ]

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
    | GetHasOverlay ->
      state,
      Cmd.OfJS.either
        js
        "Mandadin.Theme.HasOverlayControls"
        [||]
        GetHasOverlaySuccess
        Error
    | GetHasOverlaySuccess hasOverlay ->
      { state with HasOverlayControls = hasOverlay }, Cmd.none
    | CanShare ->
      state,
      Cmd.OfJS.either js "Mandadin.Share.CanShare" [||] CanShareSuccess Error
    | CanShareSuccess canShare -> { state with CanShare = canShare }, Cmd.none
    | Error err ->
      eprintfn "Update Error: [%s]" err.Message
      state, Cmd.none

  let private router = Router.infer SetView (fun m -> m.View)


  let private navigateToList (dispatch: Dispatch<Msg>) (route: string) =
    View.ListDetail route |> SetView |> dispatch

  let private goBack (dispatch: Dispatch<Msg>) () =
    SetView View.Lists |> dispatch

  let private view (state: State) (dispatch: Dispatch<Msg>) : Node =

    article [ attr.``class`` "mandadin" ] [
      if state.HasOverlayControls then
        TitleBar.View(Some state.Title)
      Navbar.View()
      main [ attr.``class`` "paper container mandadin-main" ] [
        match state.View with
        | View.Import ->
          comp<Views.Import.Page>
            [ "OnGoToListRequested"
              => Some(navigateToList dispatch) ]
            []
        | View.Notes ->
          comp<Views.Notes.Page> [ "CanShare" => state.CanShare ] []
        | View.Lists ->
          comp<Views.Lists.Page>
            [ "OnRouteRequested"
              => Some(navigateToList dispatch) ]
            []
        | View.ListDetail listId ->
          comp<Views.ListItems.Page>
            [ "ListId" => Some listId
              "CanShare" => state.CanShare
              "OnBackRequested" => Some(goBack dispatch) ]
            []
      ]
      footer [ attr.``class`` "paper row flex-spaces mandadin-footer" ] [
        p [] [
          text "\u00A9 Tunaxor Apps 2020 - 2021"
        ]
        p [] [ text "Mandadin4" ]
      ]
    ]


  type Mandadin() as this =
    inherit ProgramComponent<State, Msg>()

    override _.Program =
      let update msg state = update msg state this.JSRuntime

      Program.mkProgram init update view
      |> Program.withRouter router
#if DEBUG
      |> Program.withConsoleTrace
#endif
