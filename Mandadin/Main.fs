namespace Mandadin

open System
open Microsoft.AspNetCore.Components
open FSharp.Control.Reactive
open Bolero
open Fun.Blazor
open Fun.Blazor.Router
open Mandadin
open Mandadin.Components
open Mandadin.Views

module Main =
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

  let private listViewDetail
    (canShare: bool)
    (onBackRequested: unit -> unit)
    (listId: string)
    =
    Html.comp<ListItems.Page>
      [ "ListId" => Some listId
        "CanShare" => canShare
        "OnBackRequested" => Some(onBackRequested) ]
      []

  let private navigateToRoute
    (title: IStore<string>)
    (nav: NavigationManager)
    (listId: string)
    =
    title.Publish $"{listId} | Mandadin"
    nav.NavigateTo($"/lists/{listId}")

  let private navigateToLists
    (title: IStore<string>)
    (nav: NavigationManager)
    _
    =
    title.Publish $"{getGreeting ()} | Mandadin"
    nav.NavigateTo("/")

  let View () =
    let _view
      (
        hook: IComponentHook,
        share: IShareService,
        theme: IThemeService,
        nav: NavigationManager
      ) =
      let appTitle = hook.UseStore(getGreeting ())
      let hasOverlay = hook.UseStore false
      let canShare = hook.UseStore false

      hook.OnInitialized.Subscribe
        (fun () ->
          [ share.GetCanShare()
            |> Observable.ofTask
            |> Observable.subscribe canShare.Publish

            theme.HasOverlayControls()
            |> Observable.ofTask
            |> Observable.subscribe hasOverlay.Publish

            appTitle.Observable
            |> Observable.map theme.SetDocumentTitle
            |> Observable.switchTask
            |> Observable.subscribe ignore

            ]
          |> hook.AddDisposes)
      |> hook.AddDispose

      adaptiview () {
        let! canShare = hook.UseCVal canShare

        article () {
          class' "mandadin"

          childContent [
            html.watch (
              hasOverlay,
              fun hasOverlay ->
                if hasOverlay then
                  TitleBar.View(appTitle.Current)
                else
                  html.none
            )
            Navbar.View()
            main () {
              class' "paper container mandadin-main"

              childContent [
                html.route [
                  routeCi "/" (Lists.View())
                  routeCif
                    "/lists/%s"
                    (listViewDetail canShare (navigateToLists appTitle nav))
                  routeCi "/notes" (Notes.View canShare)
                  routeCi "/import" (Import.View())
                ]
              ]
            }
            footer () {
              class' "paper row flex-spaces mandadin-footer"

              childContent [
                p () { childContent "\u00A9 Tunaxor Apps 2020 - 2021" }
                p () { childContent "Mandadin4" }
              ]
            }
          ]
        }
      }

    html.inject ("mandadin-main-view", _view)
