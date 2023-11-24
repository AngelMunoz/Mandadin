namespace Mandadin.Client.Components

open Bolero
open Bolero.Html
open Mandadin.Client
open Microsoft.AspNetCore.Components.Routing

module Navbar =
  let collapsibleMenu =
    concat {
      input {
        attr.``class`` "toggle"
        attr.id "collapsible1"
        attr.name "collapsible1"
        attr.``type`` "checkbox"
      }

      label {
        attr.``for`` "collapsible1"

        for i in 1..3 do
          div { attr.``class`` $"bar%i{i}" }
      }
    }

  type Navbar =
    static member View
      (
        theme: Theme,
        ?onThemeChangeRequest: _ -> unit,
        ?menuItems: Node
      ) =
      let onThemeChangeRequest =
        defaultArg onThemeChangeRequest ignore

      let menuItems =
        defaultArg menuItems (empty ())

      nav {
        attr.``class`` "split-nav mandadin-navbar"

        section {
          attr.``class`` "collapsible"

          collapsibleMenu

          div {
            attr.``class`` "collapsible-body"

            ul {
              attr.``class`` "inline"
              menuItems

              li {
                attr.``class`` "cursor pointer"
                on.click onThemeChangeRequest
                textf "Tema %s" theme.AsDisplay
              }
            }
          }
        }
      }

module TitleBar =
  let View (title: string option) =
    let title = defaultArg title "Hola!"

    header {
      attr.``class`` "border mandadin-title-bar"

      navLink NavLinkMatch.All {
        attr.href "/"
        attr.``class`` "no-drag"
        text $"{title} | Mandadin"
      }
    }
