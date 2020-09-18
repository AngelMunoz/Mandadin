namespace Mandadin.Client

open Elmish
open Bolero
open Bolero.Html
open Microsoft.JSInterop
open Bolero.Remoting.Client


module Main =

  type State = { View: View; Theme: Theme }

  type Msg =
    | SetView of View
    | TryChangeTheme of Theme
    | ChangeThemeSuccess of Theme
    | Error of exn

  let private init (_: 'arg): State * Cmd<Msg> =
    {
      View = View.Notes
      Theme = Theme.Dark
    },
    Cmd.none

  let private update (msg: Msg) (state: State) (js: IJSRuntime): State * Cmd<Msg> =
    match msg with
    | SetView view -> { state with View = view }, Cmd.none
    | TryChangeTheme theme ->
        let jsThemeArg =
          match theme with
          | Theme.Dark -> "Dark"
          | _ -> "Light"

        state,
        Cmd.ofJS js "Mandadin.Theme.SwitchTheme" [| jsThemeArg |] (fun _ ->
          ChangeThemeSuccess theme) Error
    | ChangeThemeSuccess theme -> { state with Theme = theme }, Cmd.none
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
    article [ attr.``class`` "mandadin-content" ] [
      navbar state dispatch
      main [
             attr.``class`` "paper container mandadin-main"
           ] [
        match state.View with
        | View.Notes -> comp<Views.Notes.Page> [] []
        | View.Lists -> comp<Views.Lists.Page> [] []
        | View.ListDetail listId ->
            comp<Views.ListItems.Page> [ "ListId" => listId ] []
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
