namespace Mandadin.Components

open Bolero
open Fun.Blazor
open Mandadin
open Microsoft.AspNetCore.Components.Routing
open FSharp.Control.Reactive

module Navbar =

  let private collapsibleInput =
    [ input () {
        id "collapsible1"
        name "collapsible1"
        type' "checkbox"
        for' "collapsible1"
      }

      Html.label [ Html.attr.``for`` "collapsible1" ] [
        for i in 1 .. 3 do
          Html.div [ Html.attr.``class`` (sprintf "bar%i" i) ] []
      ] ]

  let private navLinks (switchTheme: _ -> unit) (theme: IStore<Theme>) =
    let themeText theme =
      match theme with
      | Theme.Light -> "Oscuro"
      | Theme.Dark -> "Claro"
      |> Html.textf "Tema %s"

    [ li () {
        childContent [
          NavLink'() {
            Match NavLinkMatch.All
            href "/notes"
            childContent "Notas"
          }
        ]
      }
      li () {
        childContent [
          NavLink'() {
            Match NavLinkMatch.All
            href "/"
            childContent "Listas"
          }
        ]
      }
      li () {
        class' "cursor pointer"
        onclick switchTheme

        childContent [
          html.watch (theme, themeText)
        ]
      } ]

  let private switchTheme
    (hook: IComponentHook)
    (themeService: IThemeService)
    (theme: IStore<Theme>)
    _
    =
    themeService.SwitchTheme(theme.Current.Inverse)
    |> Observable.ofTask
    |> Observable.subscribe (fun didChange ->
      if didChange then
        theme.Publish theme.Current.Inverse)
    |> hook.AddDispose

  let View () =
    let _view (hook: IComponentHook, themeService: IThemeService) =
      let theme = hook.UseStore Theme.Dark

      hook.OnFirstAfterRender
      |> Observable.map themeService.GetTheme
      |> Observable.switchTask
      |> Observable.subscribe theme.Publish
      |> hook.AddDispose


      nav () {
        class' "split-nav mandadin-navbar"

        childContent [
          section () {
            class' "collapsible"

            childContent [
              yield! collapsibleInput
              div () {
                class' "collapsible-body"

                childContent [
                  ul () {
                    class' "inline"

                    childContent (
                      navLinks (switchTheme hook themeService theme) theme
                    )
                  }
                ]
              }
            ]
          }
        ]
      }

    html.inject ("mandadin-nav", _view)

module TitleBar =
  let View (title: string option) =
    let appTitle = defaultArg title "Hola!"

    header () {
      class' "border mandadin-title-bar"

      childContent [
        NavLink'() {
          class' "no-drag"
          Match NavLinkMatch.All
          href "/"
          childContent $"{appTitle} | Mandadin"
        }
      ]
    }
