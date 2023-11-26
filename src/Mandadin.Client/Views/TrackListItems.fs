namespace Mandadin.Client.Views.ListItems


open Microsoft.AspNetCore.Components
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

open IcedTasks
open FsToolkit.ErrorHandling

open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting.Client

open Mandadin.Client
open Mandadin.Client.Components.TrackListItems


[<Struct>]
type UpdatableItemProp =
  | IsDone of isDone: bool
  | Name of name: string

  member this.AsString =
    match this with
    | IsDone isDone -> $"IsDone(%b{isDone})"
    | Name name -> $"Name(%s{name})"

[<Struct>]
type ItemValidationFailure =
  | EmptyItem
  | ItemExists of name: string

  member this.AsString =
    match this with
    | EmptyItem -> "The Item has no name"
    | ItemExists name -> $"ItemExists(%s{name})"

[<Struct>]
type ItemFailure =
  | ItemExists
  | FailedToUpdateItem of
    failedUpdateItem: TrackListItem *
    prop: UpdatableItemProp
  | FailedToDeleteItem of failedDeleteItem: TrackListItem
  | FailedToCreateItem of failedCreationItem: string

type Model =
  { Items: list<TrackListItem>
    TrackListId: string
    CurrentItem: string
    CanAddCurrentItem: bool
    HideDone: bool
    CanShare: bool
    ShowConfirmDeleteModal: ValueOption<TrackListItem> }

type Message =
  | SetHideDone of bool
  | SetItemList of list<TrackListItem>
  | SetNewItem of TrackListItem
  | UpdateItem of item: TrackListItem
  | RemoveItem of TrackListItem
  | SetCurrentItem of string
  | ShowConfirmDeleteModal of ValueOption<TrackListItem>
  | ItemFailure of ItemFailure


[<RequireQualifiedAccess>]
module Actions =

  let getItems (items: ITrackListItemService) itemParams dispatch _ =
    valueTaskUnit {
      let! items = items.GetItems itemParams
      items |> SetItemList |> dispatch
    }
    |> ignore

  let createItem (items: ITrackListItemService) dispatch listId newName =
    taskUnit {
      printfn "Creating item: %s on list %s" newName listId

      match! items.CreateItem(listId, newName) with
      | Ok created -> created |> SetNewItem |> dispatch
      | Error error ->
        match error with
        | EmtptyString ->
          "El objeto no puede tener un nombre vacio"
          |> FailedToCreateItem
          |> ItemFailure
          |> dispatch
        | ExistingItem name ->
          $"El objeto {name} ya existe"
          |> FailedToCreateItem
          |> ItemFailure
          |> dispatch
        | CreationFailed _ ->
          "Error al crear el objeto"
          |> FailedToCreateItem
          |> ItemFailure
          |> dispatch
    }

  let deleteItem
    (items: ITrackListItemService, dispatch)
    (item: TrackListItem)
    (confirm: Result<bool, unit>)
    =
    valueTaskUnit {
      try
        match confirm with
        | Ok true -> do! items.DeleteItem item
        | Ok _
        | Error _ -> ShowConfirmDeleteModal ValueNone |> dispatch
      with ex ->
        item
        |> FailedToDeleteItem
        |> ItemFailure
        |> dispatch
    }
    |> ignore

  let private updateItem (items: ITrackListItemService, dispatch) item prop =
    valueTaskUnit {
      let updated =
        match prop with
        | IsDone isDone -> { item with IsDone = isDone }
        | Name name -> { item with Name = name }

      try
        let! updated = items.UpdateItem updated
        updated |> UpdateItem |> dispatch
      with ex ->
        FailedToUpdateItem(item, prop)
        |> ItemFailure
        |> dispatch
    }
    |> ignore

  let updateIsDone dependencies item isDone =
    updateItem dependencies item (IsDone isDone)

  let updateName dependencies item name =
    updateItem dependencies item (Name name)

  let onHideDone (items: ITrackListItemService, dispatch) listId hideDone =
    valueTaskUnit {
      try
        do! items.SetHideDone(listId, hideDone)
        hideDone |> SetHideDone |> dispatch
      with ex ->
        hideDone |> SetHideDone |> dispatch
    }
    |> ignore

[<RequireQualifiedAccess>]
module Cmd =

  let ofGetItems (items: ITrackListItemService) listId hideDone =
    let tsk listId =
      task {
        let! items = items.GetItems(listId, hideDone)
        return items
      }

    Cmd.OfTask.perform tsk listId SetItemList

  let ofHideDone (items: ITrackListItemService, listId) =
    let tsk listId =
      task {
        let! hideDone = items.GetHideDone listId
        return hideDone
      }

    Cmd.OfTask.perform tsk listId SetHideDone

[<RequireQualifiedAccess>]
module private Components =

  let ListItem dependencies (item: TrackListItem) =
    let _, dispatch = dependencies

    li {
      attr.``class`` "listitem-item"
      attr.key item.Id

      input {
        attr.``type`` "checkbox"
        attr.``class`` "listitem-item-checkbox"
        attr.id item.Id

        bind.``checked`` item.IsDone (Actions.updateIsDone dependencies item)
      }

      input {
        bind.input.string item.Name (Actions.updateName dependencies item)
      }

      button {
        attr.``class`` "paper-btn btn-small btn-danger-outline m-0"
        on.click (fun _ -> ShowConfirmDeleteModal(ValueSome item) |> dispatch)
        Icon.Get Trash
      }
    }

  let RemoveItem dpendencies state =
    cond state.ShowConfirmDeleteModal
    <| function
      | ValueSome item ->
        let title = "Borrar Elemento."

        let subtitle =
          "Esta operacion es irreversible."

        let txt =
          $"Proceder con el borrado de '%s{item.Name}'?"

        Modals.DeleteResourceModal
          (title, subtitle, txt)
          (Actions.deleteItem dpendencies item)
      | ValueNone -> empty ()

[<RequireQualifiedAccess>]
module ListItems =

  let init (items: ITrackListItemService) (listId: string) (canShare: bool) =
    { Items = []
      TrackListId = listId
      HideDone = false
      CurrentItem = ""
      CanAddCurrentItem = false
      CanShare = canShare
      ShowConfirmDeleteModal = ValueNone },
    Cmd.ofHideDone (items, listId)

  let update
    (logger: ILogger, items: ITrackListItemService)
    (msg: Message)
    (model: Model)
    =

    match msg with
    | SetHideDone hide ->
      { model with HideDone = hide },
      Cmd.ofGetItems items model.TrackListId hide
    | SetCurrentItem item -> { model with CurrentItem = item }, Cmd.none
    | SetItemList items -> { model with Items = items }, Cmd.none
    | SetNewItem item ->
      { model with
          CurrentItem = ""
          Items =
            (item :: model.Items)
            |> List.sortBy (fun item -> item.Name) },
      Cmd.none
    | UpdateItem item ->
      { model with
          Items =
            model.Items
            |> List.map (fun i -> if i.Id = item.Id then item else i) },
      Cmd.none
    | ShowConfirmDeleteModal show ->
      { model with
          ShowConfirmDeleteModal = show },
      Cmd.none
    | RemoveItem item ->
      let items =
        model.Items
        |> List.filter (fun i -> i.Id <> item.Id)
        |> List.sortBy (fun item -> item.Name)

      { model with
          Items = items
          ShowConfirmDeleteModal = ValueNone },
      Cmd.none
    | ItemFailure(error) ->
      match error with
      | ItemExists -> logger.LogDebug("Item already exists")
      | FailedToCreateItem item ->
        logger.LogError("Failed to create item: {item}", item)
      | FailedToUpdateItem(failedUpdateItem, prop) ->
        logger.LogError(
          "Failed to update item: {failedUpdateItem} with prop: {prop}",
          failedUpdateItem,
          prop.AsString
        )
      | FailedToDeleteItem(failedDeleteItem) ->
        logger.LogDebug(
          "Failed to delete item: {failedDeleteItem}",
          failedDeleteItem
        )

      model, Cmd.none

  let view dependencies (state: Model) (dispatch: Dispatch<Message>) =
    let items, share, onBackRequested =
      dependencies

    article {
      TrackListComponents.Toolbar(state, share, onBackRequested)

      comp<NewItemForm> {
        "HideDone" => state.HideDone

        "OnSubmit"
        => Actions.createItem items dispatch state.TrackListId

        "OnHideDoneChange"
        => Actions.onHideDone (items, dispatch) state.TrackListId
      }

      Components.RemoveItem (items, dispatch) state

      ul {
        attr.``class`` "tracklist-list"

        virtualize.comp {
          virtualize.placeholder (fun _ -> div { text "Cargando..." })
          let! item = virtualize.items state.Items
          Components.ListItem (items, dispatch) item
        }

      }
    }


type Page() =
  inherit ProgramComponent<Model, Message>()

  [<Parameter>]
  member val OnBackRequested: (unit -> unit) = ignore with get, set

  [<Parameter>]
  member val ListId: ValueOption<string> = ValueNone with get, set

  [<Parameter>]
  member val CanShare: bool = false with get, set

  override this.Program =

    let items =
      this.Services.GetService<ITrackListItemService>()

    let share =
      this.Services.GetService<IShareService>()

    let loggerFactory =
      this.Services.GetService<ILoggerFactory>()

    let logger =
      loggerFactory.CreateLogger<Page>()

    let init _ =
      ListItems.init items this.ListId.Value this.CanShare

    let update =
      ListItems.update (logger, items)

    let view =
      ListItems.view (items, share, this.OnBackRequested)

    Program.mkProgram init update view
#if DEBUG
    |> Program.withConsoleTrace
#endif
