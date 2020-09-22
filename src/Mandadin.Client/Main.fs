namespace Mandadin.Client

open Elmish
open Bolero
open Bolero.Html
open Microsoft.JSInterop
open Bolero.Remoting.Client


module Main =

  type State =
    {
      View: View
      Theme: Theme
      CanShare: bool
    }

  type Msg =
    | SetView of View
    | TryChangeTheme of Theme
    | ChangeThemeSuccess of bool * Theme
    | GetTheme
    | GetThemeSuccess of string
    | CanShare
    | CanShareSuccess of bool
    | Error of exn

  let private init (_: 'arg): State * Cmd<Msg> =
    {
      View = View.Notes
      Theme = Theme.Dark
      CanShare = false
    },
    Cmd.batch [ Cmd.ofMsg GetTheme
                Cmd.ofMsg CanShare ]

  let private update (msg: Msg) (state: State) (js: IJSRuntime): State * Cmd<Msg> =
    match msg with
    | SetView view -> { state with View = view }, Cmd.none
    | TryChangeTheme theme ->
        let jsThemeArg =
          match theme with
          | Theme.Dark -> "Dark"
          | _ -> "Light"

        state,
        Cmd.ofJS js "Mandadin.Theme.SwitchTheme" [| jsThemeArg |] (fun didChange ->
          ChangeThemeSuccess(didChange, theme)) Error
    | ChangeThemeSuccess (didChange, theme) ->
        if didChange
        then { state with Theme = theme }, Cmd.none
        else state, Cmd.ofMsg (Error(exn "Failed to change theme"))
    | GetTheme ->
        state, Cmd.ofJS js "Mandadin.Theme.GetTheme" [||] GetThemeSuccess Error
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
        state, Cmd.ofJS js "Mandadin.Share.CanShare" [||] CanShareSuccess Error
    | CanShareSuccess canShare -> { state with CanShare = canShare }, Cmd.none
    | Error err ->
        eprintfn "Update Error: [%s]" err.Message
        state, Cmd.none

  let private router = Router.infer SetView (fun m -> m.View)

  let private navbar (state: State) (dispatch: Dispatch<Msg>) =
    nav [
          attr.``class`` "border fixed split-nav"
        ] [
      section [ attr.``class`` "nav-brand" ] [
        h3 [] [
          a [ router.HRef View.Notes ] [
            text "Mandadin"
          ]
        ]
      ]
      section [ attr.``class`` "collapsible" ] [
        input [ attr.id "collapsible1"
                attr.name "collapsible1"
                attr.``type`` "checkbox" ]
        label [ attr.``for`` "collapsible1" ] [
          for i in 1 .. 3 do
            div [ attr.``class`` (sprintf "bar%i" i) ] []
        ]
        div [ attr.``class`` "collapsible-body" ] [
          ul [ attr.``class`` "inline" ] [
            li [] [
              a [ router.HRef View.Notes ] [
                text "Notas"
              ]
            ]
            li [] [
              a [ router.HRef View.Lists ] [
                text "Listas"
              ]
            ]
            li [
                 attr.``class`` "cursor pointer"
                 on.click (fun _ ->
                   TryChangeTheme
                     (if state.Theme = Theme.Dark then
                       Theme.Light
                      else
                        Theme.Dark)
                   |> dispatch)
               ] [
              textf
                "Tema %s"
                (if state.Theme = Theme.Dark then "Claro" else "Oscuro")
            ]
          ]
        ]
      ]
    ]

  let private view (state: State) (dispatch: Dispatch<Msg>): Node =
    let navigateToList (route: string) =
      View.ListDetail route |> SetView |> dispatch

    let isNotesOrLists =
      state.View = View.Lists || state.View = View.Notes

    let paddedClass addPadd = if addPadd then " with-80px-pad" else ""
    article [
              attr.``class``
                (sprintf "mandadin-content%s" (paddedClass isNotesOrLists))
            ] [
      if isNotesOrLists then navbar state dispatch
      main [
             attr.``class`` "paper container mandadin-main"
           ] [
        match state.View with
        | View.Notes ->
            comp<Views.Notes.Page> [ "CanShare" => state.CanShare ] []
        | View.Lists ->
            comp<Views.Lists.Page>
              [
                "OnRouteRequested" => Some(navigateToList)
              ]
              []
        | View.ListDetail listId ->
            comp<Views.ListItems.Page>
              [
                "ListId" => Some listId
                "CanShare" => state.CanShare
              ]
              []
      ]
      footer [
               attr.``class`` "paper row flex-spaces mandadin-footer"
             ] [
        p [] [
          text "\u00A9 Tunaxor Apps 2020"
        ]
        p [] [
          text "Mandadin4"
        ]
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
