namespace Mandadin.Components

open Bolero
open Mandadin
open Microsoft.AspNetCore.Components.Routing
open Fun.Blazor

module Navbar =

  let View
    (theme: Theme)
    (onThemeChangeRequest: Theme -> unit)
    (getHref: View -> Attr)
    =
    let collapsible =
      [ input () {
          id "collapsible1"
          name "collapsible1"
          type' "checkbox"
          for' "collapsible1"

          childContent [
            Html.label [ Html.attr.``for`` "collapsible1" ] [
              for i in 1 .. 3 do
                div () { class' $"bar{i}" }
            ]
          ]
        } ]

    let listLinks =
      let getThemeText =
        textf
          "Tema %s"
          (if theme = Theme.Dark then
             "Claro"
           else
             "Oscuro")

      let onThemeItemClick _ =
        match theme with
        | Theme.Light -> onThemeChangeRequest Theme.Dark
        | _ -> onThemeChangeRequest Theme.Light

      [ li [] [
          navLink NavLinkMatch.All [ getHref View.Notes ] [ text "Notas" ]
        ]
        li [] [
          navLink NavLinkMatch.All [ getHref View.Lists ] [ text "Listas" ]
        ]
        li [ attr.``class`` "cursor pointer"
             on.click onThemeItemClick ] [
          getThemeText
        ] ]

    nav [ attr.``class`` "split-nav mandadin-navbar" ] [
      section [ attr.``class`` "collapsible" ] [
        yield! collapsible
        div [ attr.``class`` "collapsible-body" ] [
          ul [ attr.``class`` "inline" ] [
            yield! listLinks
          ]
        ]
      ]
    ]


module TitleBar =
  let View (title: string option) =
    let title = defaultArg title "Hola!"

    header [ attr.``class`` "border mandadin-title-bar" ] [
      navLink
        NavLinkMatch.All
        [ attr.href "/"
          attr.``class`` "no-drag" ]
        [ textf $"{title} | Mandadin" ]
    ]
