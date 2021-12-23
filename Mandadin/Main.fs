namespace Mandadin

open System
open Microsoft.AspNetCore.Components
open FSharp.Control.Reactive
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

  let View () =
    let _view
      (
        hook: IComponentHook,
        share: IShareService,
        theme: IThemeService
      ) =
      let appTitle = hook.UseStore(getGreeting ())
      let hasOverlay = hook.UseStore false
      let canShare = hook.UseStore false

      hook.OnFirstAfterRender.Subscribe (fun _ ->
        theme.SetDocumentTitle(getGreeting ())
        |> Async.AwaitTask
        |> Async.Start)
      |> hook.AddDispose

      hook.OnInitialized.Subscribe (fun () ->
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
                  routeCif "/lists/%s" (ListItems.View canShare)
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
