namespace Mandadin.Views

open FSharp.Control.Reactive
open FSharp.Data.Adaptive
open Microsoft.AspNetCore.Components
open Fun.Blazor
open Mandadin

[<RequireQualifiedAccess>]
module ListItems =

  let private newItemForm canAddItem onItemCreate =
    let _view (hook: IComponentHook) =
      let isDisabled = hook.UseStore true

      adaptiview () {
        let! itemName, setItemName = cval("").WithSetter()
        let! isDisabledValue = hook.UseAVal isDisabled

        let validateAndSet (event: ChangeEventArgs) =
          let itemName = unbox event.Value
          setItemName itemName

          async {
            match! canAddItem itemName with
            | true -> isDisabled.Publish false
            | false -> isDisabled.Publish true
          }

        form () {
          class' "row flex-spaces background-muted border notes-form"

          onsubmitAsync (fun _ -> onItemCreate itemName)

          childContent [
            fieldset () {
              class' "form-group"

              childContent [
                LabelBuilder() {
                  for' "current-content"
                  childContent "Nombre del objeto..."
                }
                textarea () {
                  id "current-content"
                  placeholder "Nombre del objeto..."
                  value itemName
                  oninputAsync validateAndSet
                }
              ]
            }
            button () {
              type' "submit"
              class' "paper-btn btn-small"
              disabled isDisabledValue
              childContent (Icon.Get Save None)
            }
          ]
        }
      }

    html.inject ("mandadin-tracklist-item-form", _view)

  let private listItem
    canAddItem
    onItemUpdate
    onItemDelete
    (item: TrackListItem)
    =
    li () {
      class' "listitem-item"
      key item.Id

      childContent [
        fieldset () {
          class' "form-group"
          style' "display: flex;"

          childContent [
            LabelBuilder() {
              class' "paper-check"

              childContent [
                input () {
                  type' "checkbox"
                  class' "listitem-item-checkbox"
                  id item.Id
                  key item.Id
                  checked' item.IsDone

                  onchangeAsync (fun _ ->
                    onItemUpdate ({ item with IsDone = not <| item.IsDone }))
                }
                SpanBuilder() { childContent "" }
              ]
            }
            input () {
              value item.Name

              oninputAsync (fun event ->
                async {
                  match! canAddItem (unbox event.Value) with
                  | true ->
                    return!
                      onItemUpdate ({ item with Name = unbox event.Value })
                  | false -> ()
                })
            }
            button () {
              class' "paper-btn btn-small btn-danger-outline m-0"
              onclick (fun _ -> onItemDelete item)
              childContent (Icon.Get Trash None)
            }
          ]
        }
      ]
    }

  let private toolbar
    onToShare
    onToClipboard
    onGoBack
    canShare
    (hideDoneCheckbox: Bolero.Node)
    (tracklistId: string)
    =

    div () {
      class' "border"

      childContent [
        section () {
          class' "column-center"

          childContent [
            h4 () { childContent tracklistId }
            fieldset () {
              class' "form-group"
              childContent hideDoneCheckbox
            }
          ]
        }
        section () {
          class' "row flex-center"

          childContent [
            button () {
              class' "paper-btn btn-small"
              onclick onGoBack
              childContent (Icon.Get Back None)
            }
            if canShare then
              button () {
                class' "paper-btn btn-small"
                onclick onToShare
                childContent (Icon.Get Share None)
              }
            button () {
              class' "paper-btn btn-small"
              onclick onToClipboard
              childContent (Icon.Get Copy None)
            }

            ]
        }
      ]
    }

  let private _view
    (canShare: bool)
    (tracklistId: string)
    (hook: IComponentHook,
     listItems: ITrackListItemService,
     lists: ITrackListService,
     nav: NavigationManager)
    =
    let hideDone = hook.UseStore false
    let items = hook.UseStore []
    let isDeleting = hook.UseStore None

    let onDeleteModalResult item (result: Result<bool, unit>) =
      match result with
      | Ok true ->
        async {
          let! result = listItems.Delete item |> Async.AwaitTask

          items.Current
          |> List.filter (fun item -> item.Id <> result.Id)
          |> items.Publish

          isDeleting.Publish None
        }
        |> Async.Start
      | Ok false
      | Error _ -> isDeleting.Publish(None)

    let deleteModal (item: TrackListItem option) =

      match item with
      | Some item ->
        let title = "Borrar Elemento"
        let subtitle = "esta operacion es irreversible"

        let txt =
          sprintf """Proceder con el borrado de "%s"?""" item.Name

        Modals.DeleteResourceModal
          (title, subtitle, txt)
          true
          (onDeleteModalResult item)
      | None -> html.none

    let canAddItem name =
      async {
        let! itemExists =
          listItems.Exists(tracklistId, name)
          |> Async.AwaitTask

        return not itemExists
      }

    let onItemUpdate item =
      async {
        let! result = listItems.Update item |> Async.AwaitTask

        match hideDone.Current, result.IsDone with
        | true, true ->

          items.Current
          |> List.filter (fun item -> item.Id <> result.Id)
          |> items.Publish
        | _ ->
          let newItems =
            items.Current
            |> List.map (fun item ->
              if item.Id <> result.Id then
                item
              else
                result)

          items.Publish newItems
      }

    let onItemCreate item =
      async {
        let! result =
          listItems.Create(tracklistId, item)
          |> Async.AwaitTask

        items.Publish (fun items ->
          (result :: items)
          |> List.sortBy (fun item -> item.Name))
      }

    let onItemDelete item = isDeleting.Publish(Some item)

    let onHideDone areHidden =
      async {
        let! result =
          lists.SaveHideDone(tracklistId, areHidden)
          |> Async.AwaitTask

        hideDone.Publish areHidden

        match result.hideDone with
        | true ->
          let filterOutDone item = not item.IsDone

          items.Current
          |> List.filter filterOutDone
          |> items.Publish
        | false ->
          let! result =
            listItems.Find(tracklistId, false)
            |> Async.AwaitTask

          result
          |> List.sortBy (fun item -> item.Name)
          |> items.Publish
      }

    let onToShare _ =
      listItems.ShareItems(tracklistId, items.Current)
      |> Async.AwaitTask
      |> Async.Ignore
      |> Async.Start

    let onToClipboard _ =
      listItems.SendToClipboard items.Current
      |> Async.AwaitTask
      |> Async.Ignore
      |> Async.Start

    let onGoBack _ = nav.NavigateTo("/")

    hook.OnInitialized
    |> Observable.map (fun _ -> lists.GetHideDone tracklistId)
    |> Observable.switchTask
    |> Observable.map (fun result ->
      hideDone.Publish result
      listItems.Find(tracklistId, result))
    |> Observable.switchTask
    |> Observable.subscribe (fun result ->
      items.Publish(result |> List.sortBy (fun item -> item.Name)))
    |> hook.AddDispose

    adaptiview () {
      let! hideDone = hook.UseAVal hideDone
      let! items = hook.UseAVal items

      let hideDoneCheckbox =
        LabelBuilder() {
          for' "paperCheck1"
          class' "paper-check"

          childContent [
            input () {
              id "paperCheck1"
              name "paperCheck"
              type' "checkbox"
              checked' hideDone
              onchangeAsync (fun _ -> onHideDone (not <| hideDone))
            }
            SpanBuilder() { childContent "Esconder Terminados" }
          ]
        }

      article () {
        childContent [
          toolbar
            onToShare
            onToClipboard
            onGoBack
            canShare
            hideDoneCheckbox
            tracklistId
          newItemForm canAddItem onItemCreate
          html.watch (isDeleting, deleteModal)
          ul () {
            class' "tracklist-list"

            childContent (
              Virtualize'() {
                Items(items |> Array.ofList)
                ChildContent(listItem canAddItem onItemUpdate onItemDelete)
              }
            )
          }
        ]
      }
    }

  let View canShare tracklistId =
    html.inject ("mandadin-tracklist-items-view", _view canShare tracklistId)
