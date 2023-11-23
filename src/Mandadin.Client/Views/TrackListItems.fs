namespace Mandadin.Client.Views

open Elmish
open Microsoft.JSInterop
open Bolero
open Bolero.Html
open Bolero.Remoting.Client
open Mandadin.Client
open Microsoft.AspNetCore.Components


[<RequireQualifiedAccess>]
module ListItems =
  let stringifyItems (items: list<TrackListItem>) : string =
    let isDoneToX (isDone: bool) = if isDone then 'x' else ' '

    let stringified =
      items
      |> Array.ofList
      |> Array.Parallel.map (fun item ->
        sprintf "[ %c ] %s" (isDoneToX item.IsDone) item.Name)

    System.String.Join('\n', stringified)

  type State =
    { Items: list<TrackListItem>
      TrackListId: ValueOption<string>
      CurrentItem: string
      CanAddCurrentItem: bool
      HideDone: bool
      CanShare: bool
      ShowConfirmDeleteModal: ValueOption<TrackListItem> }


  type UpdatableItemProp =
    | IsDone of bool
    | Name of string

  type Msg =
    | GoBack
    | SetCurrentItem of string

    | GetItems
    | GetItemsSuccess of seq<TrackListItem>

    | ValidateItem of string
    | ValidateItemSuccess of itemExists: bool * name: string

    | ValidateExisting of TrackListItem
    | ValidateExistingSuccess of itemExists: bool * item: TrackListItem
    | UpdateItemProp of item: TrackListItem * prop: UpdatableItemProp
    | UpdateItemPropSuccess of TrackListItem

    | DeleteItem of TrackListItem
    | DeleteItemSuccess of TrackListItem

    | CreateItem of string
    | CreateItemSuccess of TrackListItem

    | ShowConfirmDeleteModal of ValueOption<TrackListItem>
    | ShowConfirmDeleteModalAction of TrackListItem * Result<bool, unit>

    | ShareRequest of list<TrackListItem>
    | ShareRequestSuccess

    | ToClipboard of list<TrackListItem>
    | ToClipboardSuccess

    | RequestHideDone
    | RequestHideDoneSuccess of bool

    | SaveHideDone
    | SaveHideDoneSuccess

    | HideDone of bool

    | Error of exn


  let init (listId: ValueOption<string>) (canShare: bool) =
    { Items = []
      TrackListId = listId
      HideDone = false
      CurrentItem = ""
      CanAddCurrentItem = false
      CanShare = canShare
      ShowConfirmDeleteModal = ValueNone },
    Cmd.batch [ Cmd.ofMsg RequestHideDone ]

  let update
    (msg: Msg)
    (state: State)
    (onGoBackRequested: unit -> unit)
    (js: IJSRuntime)
    =
    let emptyListId =
      state, Cmd.ofMsg (Error(exn "ListId cannot be Empty"))

    match msg with
    | GoBack ->
      onGoBackRequested ()
      state, Cmd.none
    | HideDone hide ->
      { state with HideDone = hide },
      Cmd.batch
        [ Cmd.ofMsg GetItems
          Cmd.ofMsg SaveHideDone ]
    | SetCurrentItem item ->
      { state with CurrentItem = item }, Cmd.ofMsg (ValidateItem(item))
    | RequestHideDone ->
      let listId =
        defaultValueArg state.TrackListId "X"

      state,
      Cmd.OfJS.either
        js
        "Mandadin.Database.GetHideDone"
        [| listId |]
        RequestHideDoneSuccess
        (fun _ -> GetItems)
    | RequestHideDoneSuccess hideDone ->
      { state with HideDone = hideDone }, Cmd.ofMsg GetItems
    | SaveHideDone ->
      let listId =
        defaultValueArg state.TrackListId "X"

      state,
      Cmd.OfJS.either
        js
        "Mandadin.Database.SaveHideDone"
        [| listId; state.HideDone |]
        (fun _ -> SaveHideDoneSuccess)
        Error
    | SaveHideDoneSuccess -> state, Cmd.none
    | GetItems ->
      match state.TrackListId with
      | ValueSome listId ->
        state,
        Cmd.OfJS.either
          js
          "Mandadin.Database.GetListItems"
          [| listId; state.HideDone |]
          GetItemsSuccess
          Error
      | ValueNone -> emptyListId
    | GetItemsSuccess list ->
      { state with
          Items =
            list
            |> Seq.sortBy (fun item -> item.Name)
            |> List.ofSeq },
      Cmd.none
    | ValidateItem item ->
      match state.TrackListId with
      | ValueSome listid ->
        let onSuccess nameExists = ValidateItemSuccess(nameExists, item)

        state,
        Cmd.OfJS.either
          js
          "Mandadin.Database.ListItemExists"
          [| listid; state.CurrentItem |]
          onSuccess
          Error
      | ValueNone -> emptyListId
    | ValidateItemSuccess(nameExists, itemName) ->
      let canAdd =
        not nameExists && itemName.Length <> 0

      { state with
          CanAddCurrentItem = canAdd },
      Cmd.none

    | CreateItem item ->
      match state.TrackListId with
      | ValueSome listid ->
        state,
        Cmd.OfJS.either
          js
          "Mandadin.Database.CreateListItem"
          [| listid; item |]
          CreateItemSuccess
          Error
      | ValueNone -> emptyListId
    | CreateItemSuccess item ->
      { state with
          Items =
            (item :: state.Items)
            |> List.sortBy (fun item -> item.Name) },
      Cmd.none
    | UpdateItemProp(item, prop) ->
      match prop with
      | IsDone isDone ->
        state,
        Cmd.OfJS.either
          js
          "Mandadin.Database.UpdateListItem"
          [| { item with IsDone = isDone } |]
          UpdateItemPropSuccess
          Error
      | Name name ->
        state, Cmd.ofMsg (ValidateExisting { item with Name = name })
    | ValidateExisting item ->
      state,
      Cmd.OfJS.either
        js
        "Mandadin.Database.ListItemExists"
        [| item.ListId; item.Name |]
        (fun exists -> ValidateExistingSuccess(exists, item))
        Error
    | ValidateExistingSuccess(exists, item) ->
      match exists with
      | true -> state, Cmd.none
      | false ->
        state,
        Cmd.OfJS.either
          js
          "Mandadin.Database.UpdateListItem"
          [| item |]
          UpdateItemPropSuccess
          Error
    | UpdateItemPropSuccess item ->
      let items =
        match state.HideDone, item.IsDone with
        | true, true ->
          state.Items
          |> List.filter (fun i -> i.Id <> item.Id)
        | _ ->
          state.Items
          |> List.map (fun i -> if i.Id = item.Id then item else i)


      { state with
          Items = items |> List.sortBy (fun item -> item.Name) },
      Cmd.none
    | ShowConfirmDeleteModal show ->
      { state with
          ShowConfirmDeleteModal = show },
      Cmd.none
    | ShowConfirmDeleteModalAction(item, result) ->
      let cmd =
        match result with
        | Ok result when result -> Cmd.ofMsg (DeleteItem item)
        | _ -> Cmd.ofMsg (ShowConfirmDeleteModal ValueNone)

      state, cmd
    | DeleteItem item ->
      state,
      Cmd.OfJS.either
        js
        "Mandadin.Database.DeleteListItem"
        [| item |]
        DeleteItemSuccess
        Error
    | DeleteItemSuccess item ->
      let items =
        state.Items
        |> List.filter (fun i -> i.Id <> item.Id)
        |> List.sortBy (fun item -> item.Name)

      { state with
          Items = items
          ShowConfirmDeleteModal = ValueNone },
      Cmd.none

    | ShareRequest items ->
      let stringified = stringifyItems items

      let idValue =
        defaultValueArg state.TrackListId "Mandadin"

      state,
      Cmd.OfJS.either
        js
        "Mandadin.Share.ShareContent"
        [| idValue; stringified |]
        (fun _ -> ShareRequestSuccess)
        Error
    | ShareRequestSuccess -> state, Cmd.none
    | ToClipboard items ->
      let stringified = stringifyItems items

      state,
      Cmd.OfJS.either
        js
        "Mandadin.Clipboard.CopyTextToClipboard"
        [| stringified |]
        (fun _ -> ShareRequestSuccess)
        Error
    | ToClipboardSuccess -> state, Cmd.none
    | Error ex ->
      eprintfn "Update Error [%s]" ex.Message
      state, Cmd.none

  let private newItemForm (state: State) (dispatch: Dispatch<Msg>) =
    let currentContentTxt =
      "Nombre del objeto..."

    form {
      attr.``class`` "row flex-spaces background-muted border notes-form"
      on.submit (fun _ -> CreateItem state.CurrentItem |> dispatch)

      fieldset {
        attr.``class`` "form-group"

        label {
          attr.``for`` "current-content"
          text currentContentTxt
        }

        textarea {
          attr.id "current-content"
          attr.placeholder currentContentTxt
          bind.input.string state.CurrentItem (SetCurrentItem >> dispatch)
        }

        label {
          attr.``for`` "paperCheck1"
          attr.``class`` "paper-check"

          input {
            attr.id "paperCheck1"
            attr.name "paperChecks"
            attr.``type`` "checkbox"
            bind.``checked`` state.HideDone (HideDone >> dispatch)
          }

          span { text "Esconder Terminados" }
        }
      }

      button {
        attr.``type`` "submit"
        attr.``class`` "paper-btn btn-small"
        attr.disabled (not state.CanAddCurrentItem)
        Icon.Get Save
      }
    }


  let inline private listItem (dispatch: Dispatch<Msg>) (item: TrackListItem) =
    li {
      attr.``class`` "listitem-item"
      attr.key item.Id

      input {
        attr.``type`` "checkbox"
        attr.``class`` "listitem-item-checkbox"
        attr.id item.Id

        bind.``checked`` item.IsDone (fun isDone ->
          UpdateItemProp(item, (IsDone isDone)) |> dispatch)
      }

      input {
        bind.input.string item.Name (fun name ->
          ValidateExisting { item with Name = name }
          |> dispatch)
      }

      button {
        attr.``class`` "paper-btn btn-small btn-danger-outline m-0"
        on.click (fun _ -> ShowConfirmDeleteModal(ValueSome item) |> dispatch)
        Icon.Get Trash
      }
    }

  let toolbar (state: State) (dispatch: Dispatch<Msg>) =
    let getId =
      defaultValueArg state.TrackListId ""

    div {
      attr.``class`` "border"

      section {
        attr.``class`` "row flex-center"
        h4 { text getId }
      }

      section {
        attr.``class`` "row flex-center"

        button {
          attr.``class`` "paper-btn btn-small"
          on.click (fun _ -> dispatch GoBack)
          Icon.Get Back
        }

        cond state.CanShare
        <| function
          | true ->
            button {
              attr.``class`` "paper-btn btn-small"
              on.click (fun _ -> ShareRequest state.Items |> dispatch)
              Icon.Get Share
            }
          | false -> empty ()

        button {
          attr.``class`` "paper-btn btn-small"
          on.click (fun _ -> ToClipboard state.Items |> dispatch)
          Icon.Get Copy
        }
      }
    }

  let view (state: State) (dispatch: Dispatch<Msg>) =
    let deleteModal (item: TrackListItem) =
      let title = "Borrar Elemento"

      let subtitle =
        "esta operacion es irreversible"

      let txt =
        sprintf """Proceder con el borrado de "%s"?""" item.Name

      let showModal =
        state.ShowConfirmDeleteModal.IsSome

      Modals.DeleteResourceModal (title, subtitle, txt) showModal (fun result ->
        ShowConfirmDeleteModalAction(item, result)
        |> dispatch)

    article {
      toolbar state dispatch
      newItemForm state dispatch

      cond state.ShowConfirmDeleteModal
      <| function
        | ValueSome value -> deleteModal value
        | ValueNone -> empty ()

      ul {
        attr.``class`` "tracklist-list"

        virtualize.comp {
          virtualize.placeholder (fun _ -> div { text "Cargando..." })
          let! item = virtualize.items state.Items
          listItem dispatch item
        }

      }
    }


  type Page() =
    inherit ProgramComponent<State, Msg>()

    [<Parameter>]
    member val OnBackRequested: (unit -> unit) = id with get, set

    [<Parameter>]
    member val ListId: ValueOption<string> = ValueNone with get, set

    [<Parameter>]
    member val CanShare: bool = false with get, set

    override this.Program =
      let init _ = init this.ListId this.CanShare

      let update msg state =
        update msg state this.OnBackRequested this.JSRuntime

      Program.mkProgram init update view
#if DEBUG
      |> Program.withConsoleTrace
#endif
