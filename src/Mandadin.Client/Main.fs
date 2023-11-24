namespace Mandadin.Client

open System

open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Routing
open Microsoft.JSInterop

open IcedTasks

open Elmish

open Bolero
open Bolero.Html
open Bolero.Remoting.Client

open Mandadin.Client.Components
open Mandadin.Client.Components.Navbar
open Mandadin.Client.Router

[<Struct>]
type AppInit =
  { CanShare: bool
    Theme: Theme
    HasOverlay: bool
    Title: string }

[<Struct; NoComparison; NoEquality>]
type AppDependencies =
  { jsRuntime: IJSRuntime
    logger: ILogger }

[<Struct>]
type ActionError =
  | ThemeChangeFailed of targetTheme: Theme
  | TitleChangeFailed of targetTitle: string
  | SetViewFailed of targetPage: Page
  | InitializationFailed of initError: string

type Model =
  { Page: Page
    Theme: Theme
    Title: string
    CanShare: bool
    HasOverlayControls: bool }

[<Struct>]
type Message =
  | SetView of view: Page
  | SetTheme of theme: Theme
  | SetTitle of title: string
  | SetInitial of init: AppInit
  | NotifyFailure of failure: ActionError

[<AutoOpen>]
module Mandadin =

  let (|Morning|Evening|Night|Unknown|) =
    function
    | num when num > 4 && num < 12 -> Morning
    | num when num > 11 && num < 20 -> Evening
    | num when num > 19 || num < 5 -> Night
    | num -> Unknown num

  let inline getGreeting () =
    let hour = DateTime.Now.Hour

    match hour with
    | Morning -> "Buenos dias!"
    | Evening -> "Buenas tardes!"
    | Night -> "Buenas noches!"
    | Unknown num ->
      printfn $"{num}"
      "Hola!"

  let inline navigateToList dispatch (route: string) =
    Page.ListDetail route |> SetView |> dispatch

  let inline goBack dispatch () = SetView Page.Lists |> dispatch

  let onThemeChangeRequest
    { jsRuntime = jsRuntime
      logger = logger }
    (state: Model)
    dispatch
    _
    =
    valueTaskUnit {
      let newTheme =
        match state.Theme with
        | Theme.Dark -> Theme.Light
        | Theme.Light -> Theme.Dark

      try
        logger.LogDebug("Requesting New Theme: {value}", newTheme.AsDisplay)

        let! value =
          jsRuntime.InvokeAsync<bool>(
            "Mandadin.Theme.SwitchTheme",
            [| box newTheme.AsString |]
          )

        logger.LogDebug("Accepted Theme: {value}", value)

        if value then
          SetTheme newTheme |> dispatch
        else
          NotifyFailure(ThemeChangeFailed newTheme)
          |> dispatch
      with ex ->
        logger.LogWarning("Failed to get theme: {error}", ex.Message)

        NotifyFailure(ThemeChangeFailed newTheme)
        |> dispatch

      return ()
    }
    |> ignore

  let tryGetHasOverlay
    { jsRuntime = jsRuntime
      logger = logger }
    =
    cancellableValueTask {
      let! token = CancellableValueTask.getCancellationToken ()

      try
        let! value =
          jsRuntime.InvokeAsync<bool>(
            "Mandadin.Theme.HasOverlayControls",
            token,
            [||]
          )

        logger.LogDebug("Overlay status: {value}", value)
        return false
      with ex ->
        logger.LogWarning("Failed to get overlay status: {error}", ex.Message)

        return false
    }

  let tryGetTheme
    { jsRuntime = jsRuntime
      logger = logger }
    =
    cancellableValueTask {
      let! token = CancellableValueTask.getCancellationToken ()

      try
        let! value =
          jsRuntime.InvokeAsync<string>("Mandadin.Theme.GetTheme", token, [||])

        logger.LogDebug("Theme: {value}", value)
        return value |> Theme.ofString
      with ex ->
        logger.LogWarning("Failed to get theme: {error}", ex.Message)

        return Theme.Dark
    }

  let tryGetCanShare
    { jsRuntime = jsRuntime
      logger = logger }
    =
    cancellableValueTask {
      let! token = CancellableValueTask.getCancellationToken ()

      try
        let! value =
          jsRuntime.InvokeAsync<bool>("Mandadin.Share.CanShare", token, [||])

        logger.LogDebug("Share status: {value}", value)
        return value
      with ex ->
        logger.LogWarning("Failed to get share status: {error}", ex.Message)

        return false
    }

  let setInitialParams dependencies =
    cancellableTask {
      let! hasOverlay = tryGetHasOverlay dependencies

      let! theme = tryGetTheme dependencies

      let! canShare = tryGetCanShare dependencies

      return
        { CanShare = canShare
          Theme = theme
          HasOverlay = hasOverlay
          Title = getGreeting () }
    }

type AppShell() =
  inherit ProgramComponent<Model, Message>()

  let cancellationTokenSource =
    new Threading.CancellationTokenSource()

  let init dependencies cancellationToken =
    { Page = Page.Notes
      Theme = Theme.Dark
      CanShare = false
      HasOverlayControls = false
      Title = getGreeting () },
    Cmd.OfTask.either
      (setInitialParams dependencies)
      cancellationToken
      SetInitial
      (fun err -> NotifyFailure(InitializationFailed err.Message))


  let update { jsRuntime = _; logger = logger } message model =
    match message with
    | SetView view -> { model with Page = view }, Cmd.none
    | SetTheme theme -> { model with Theme = theme }, Cmd.none
    | SetTitle title -> { model with Title = title }, Cmd.none
    | SetInitial { CanShare = canShare
                   Theme = theme
                   HasOverlay = hasOverlay
                   Title = title } ->
      { model with
          CanShare = canShare
          HasOverlayControls = hasOverlay },
      Cmd.batch
        [ Cmd.ofMsg (SetTheme theme)
          Cmd.ofMsg (SetTitle title) ]
    | NotifyFailure err ->
      match err with
      | ThemeChangeFailed targetTheme ->
        logger.LogDebug("Theme Change Error: {error}", targetTheme)
        model, Cmd.none
      | TitleChangeFailed targetTitle ->
        logger.LogDebug("Title Change Error: {error}", targetTitle)
        model, Cmd.none
      | SetViewFailed targetPage ->
        logger.LogDebug("Set View Error: {error}", targetPage)
        model, Cmd.none
      | InitializationFailed err ->
        logger.LogDebug("Initialization Error: {error}", err)
        model, Cmd.none

  let view dependencies state dispatch =
    article {
      attr.``class`` "mandadin"

      cond state.HasOverlayControls
      <| function
        | true -> TitleBar.View(Some state.Title)
        | false -> empty ()

      Navbar.View(
        state.Theme,
        onThemeChangeRequest dependencies state dispatch,
        concat {
          Router.navLink<AppShell, _, _> (Page.Notes, "Notas")

          Router.navLink<AppShell, _, _> (
            Page.Lists,
            "Listas",
            linkMatch = NavLinkMatch.Prefix
          )
        }
      )

      main {
        attr.``class`` "paper container mandadin-main"

        cond state.Page
        <| function
          | Page.Import ->
            comp<Views.Import.Page> {
              "OnGoToListRequested" => navigateToList dispatch
            }
          | Page.Notes ->
            comp<Views.Notes.Page> { "CanShare" => state.CanShare }
          | Page.Lists ->
            comp<Views.Lists.Page> {
              "OnRouteRequested" => navigateToList dispatch
            }
          | Page.ListDetail listId ->
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

  static member val Router = Router.infer SetView (fun m -> m.Page)

  [<Inject>]
  member val LoggerFactory: ILoggerFactory =
    Unchecked.defaultof<ILoggerFactory> with get, set

  override this.Program =
    let dependencies =
      { jsRuntime = this.JSRuntime
        logger = this.LoggerFactory.CreateLogger("AppShell") }

    let view = view dependencies
    let update = update dependencies

    let init _ =
      init dependencies cancellationTokenSource.Token

    Program.mkProgram init update view
    |> Program.withRouter AppShell.Router
#if DEBUG
    |> Program.withConsoleTrace
#endif


  interface IDisposable with
    member _.Dispose() =
      if not cancellationTokenSource.IsCancellationRequested then
        cancellationTokenSource.Cancel()

      cancellationTokenSource.Dispose()
