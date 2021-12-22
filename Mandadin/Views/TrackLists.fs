namespace Mandadin.Views

open Microsoft.AspNetCore.Components
open Fun.Blazor
open FSharp.Control.Reactive
open Mandadin

[<RequireQualifiedAccess>]
module Lists =
  let private trackListForm
    (tracklists: ITrackListService)
    (clipboard: IClipboardService)
    (nav: NavigationManager)
    (hook: IComponentHook)
    =
    let listTitle = hook.UseStore ""
    let clipboardContent = hook.UseStore ""
    let isDisabled = hook.UseStore true
    let showImportDialog = hook.UseStore false

    let onFormSubmit (store: IStore<string>) _ =
      async {
        let! result =
          tracklists.CreateList store.Current
          |> Async.AwaitTask

        nav.NavigateTo($"/lists/{result.Id}")
      }

    let getFromClipboard _ =
      async {
        let! content = clipboard.GetFromClipboard() |> Async.AwaitTask
        clipboardContent.Publish content
        showImportDialog.Publish true
      }

    let importResult (result: Result<string * string, unit>) =
      match result with
      | Ok (title, content) ->
        let parsed = Import.parseContentString content

        async {
          let! list =
            tracklists.ImportList(title, parsed)
            |> Async.AwaitTask

          nav.NavigateTo($"lists/{list.Id}")
        }
        |> Async.Start
      | Result.Error _ ->
        showImportDialog.Publish false
        clipboardContent.Publish ""

    let setListTitle (store: IStore<string>) (event: ChangeEventArgs) =
      async {
        let value =
          let value: string = unbox event.Value
          value.Trim()

        store.Publish value

        match! (tracklists.ListNameExists value)
               |> Async.AwaitTask with
        | true -> isDisabled.Publish true
        | false -> isDisabled.Publish false
      }

    adaptiview () {
      let! listTitleValue = hook.UseAVal listTitle
      let! isDisabled = hook.UseAVal isDisabled
      let! showImportDialog = hook.UseAVal showImportDialog
      let! clipboardContent = hook.UseAVal clipboardContent

      if showImportDialog then
        Modals.ImportTrackList
          showImportDialog
          importResult
          (Some listTitleValue)
          (Some clipboardContent)

      form () {
        class' "row flex-spaces background-muted border notes-form"
        onsubmitAsync (onFormSubmit listTitle)

        childContent [
          fieldset () {
            class' "form-group"

            childContent [
              elt.label () {
                for' "current-content"
                childContent "Nombre de la nueva lista..."
              }
              textarea () {
                id "current-content"
                placeholder "Nombre de la nueva lista..."
                oninputAsync (setListTitle listTitle)
                value listTitleValue
              }
            ]
          }
          button () {
            type' "submit"
            disabled isDisabled
            childContent (Icon.Get Save None)
          }
          button () {
            class' "paper-btn btn-small"
            type' "button"
            onclickAsync getFromClipboard
            childContent (Icon.Get Import None)
          }
        ]
      }
    }

  let private listItem navigateTo deleteItem (item: TrackList) =
    li () {
      class' "tracklist-item row flex-spaces"
      key item.Id

      childContent [
        p () {
          class' "m-05"
          childContent item.Id
        }
        button () {
          class' "paper-btn btn-small btn-primary-outline"
          onclick (navigateTo item)
          childContent (Icon.Get Text None)
        }
        button () {
          class' "paper-btn btn-small btn-danger-outline"
          onclick (deleteItem item)
          childContent (Icon.Get Trash None)
        }

        ]
    }

  let private _view
    (
      hook: IComponentHook,
      tracklistService: ITrackListService,
      clipboard: IClipboardService,
      nav: NavigationManager
    ) =
    let items = hook.UseStore []
    let isDeleting = hook.UseStore None

    hook.OnInitialized
    |> Observable.map tracklistService.FindLists
    |> Observable.switchTask
    |> Observable.subscribe items.Publish
    |> hook.AddDispose

    let onDeleteModalResult (item: TrackList) result =
      match result with
      | Ok true ->
        async {
          do!
            tracklistService.DeleteList(item.Id, item.Rev)
            |> Async.AwaitTask

          items.Publish
            (fun existing ->
              existing
              |> List.filter (fun existing -> item.Id <> existing.Id))

          isDeleting.Publish None
        }
        |> Async.Start
      | Ok false
      | Result.Error _ -> isDeleting.Publish None

    let deleteModal (item: TrackList option) =
      match item with
      | Some item ->
        let title = "Borrar Elemento"
        let subtitle = "esta operacion es irreversible"

        let txt =
          sprintf """Proceder con el borrado de "%s"?""" item.Id

        Modals.DeleteResourceModal
          (title, subtitle, txt)
          true
          (onDeleteModalResult item)
      | None -> html.none

    let onNavigateTo (item: TrackList) _ = nav.NavigateTo($"/lists/{item.Id}")

    let onDeleteItem item _ = isDeleting.Publish(Some item)

    adaptiview () {
      let! items = hook.UseAVal items

      article () {
        childContent [
          html.inject (
            "mandadin-tracklist-form",
            trackListForm tracklistService clipboard nav
          )

          html.watch (isDeleting, deleteModal)
          ul () {
            class' "tracklist-list child-borders"

            childContent (
              Virtualize'() {
                Items(items |> Array.ofList)
                ChildContent(listItem onNavigateTo onDeleteItem)
              }
            )
          }
        ]
      }
    }

  let View () =
    html.inject ("mandadin-tracklist-view", _view)
