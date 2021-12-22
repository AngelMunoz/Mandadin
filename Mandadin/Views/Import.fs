namespace Mandadin.Views

open Elmish
open Microsoft.JSInterop
open Bolero
open Bolero.Remoting.Client
open Microsoft.AspNetCore.Components
open FSharp.Control.Reactive
open Fun.Blazor
open Mandadin


[<RequireQualifiedAccess>]
module Import =

  let parseContentString (content: string) : array<array<obj>> =
    let parseRow (row: string) =
      let split = row.Split(" ] ")

      let isDone =
        match split |> Array.tryItem 0 with
        | Some content -> content.Contains('x')
        | None -> false

      let nameStr =
        match split |> Array.tryItem 1 with
        | Some content -> content.Trim()
        | None -> "Error de linea"

      [| box isDone; box nameStr |]

    content.Split('\n') |> Array.Parallel.map parseRow

  let View () =

    let _view
      (
        hook: IComponentHook,
        share: IShareService,
        trackLists: ITrackListService,
        nav: NavigationManager
      ) =

      let store =
        hook.UseStore<ShareDataPayload option> None

      hook.OnInitialized
      |> Observable.map (fun _ ->
        task {
          try
            let! res = share.ImportShareContent()
            return Some res
          with
          | ex -> return None
        })
      |> Observable.switchTask
      |> Observable.subscribe store.Publish
      |> hook.AddDispose

      let importContent (res: Result<string * string, unit>) =
        task {
          match res with
          | Ok (title, content) ->
            let items = parseContentString content
            let! list = trackLists.ImportList(title, items)
            return Some list
          | Result.Error _ -> return None
        }
        |> Observable.ofTask
        |> Observable.subscribe (fun item ->
          item
          |> Option.iter (fun item -> nav.NavigateTo($"/lists/{item.Id}")))
        |> hook.AddDispose


      let getStoreContent (store: IStore<ShareDataPayload option>) =
        let _content content =
          match content with
          | Some content ->
            Modals.ImportTrackList
              true
              importContent
              (Some content.Title)
              (Some content.Text)
          | None ->
            p () {
              childContent
                "No pudimos obtener informacion de lo que nos querias compartir 😢"
            }

        html.watch (store, _content)


      article () {
        childContent [
          a () {
            class' "paper-btn btn-small"
            href "/"
            childContent (Icon.Get Back None)
          }
          getStoreContent store
        ]
      }

    html.inject ("mandadin-impport-view", _view)
