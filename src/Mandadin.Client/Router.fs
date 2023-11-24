namespace Mandadin.Client

open Microsoft.AspNetCore.Components.Routing

open Bolero
open Bolero.Html

module Router =

  type IRouterContainer<'Container, 'Model, 'Msg
    when 'Container: (static member Router: Router<Page, 'Model, 'Msg>)> =
    'Container

  let inline private navLink<'Container, 'Model, 'Msg
    when IRouterContainer<'Container, 'Msg, 'Model>>
    view
    label
    classes
    hash
    linkMatch
    =
    let href =
      'Container.Router.HRef(view, defaultArg hash null)

    li {
      navLink (defaultArg linkMatch NavLinkMatch.All) {
        attr.``class`` (defaultArg classes "")
        href
        text label
      }
    }

  type Router =

    static member inline navLink<'Container, 'Model, 'Msg
      when IRouterContainer<'Container, 'Model, 'Msg>>
      (
        view: Page,
        label: string,
        ?classes: string,
        ?hash: string,
        ?linkMatch: NavLinkMatch
      ) =
      navLink<'Container, 'Msg, 'Model> view label classes hash linkMatch
