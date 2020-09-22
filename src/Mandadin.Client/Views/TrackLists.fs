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
  type State =
    {
      Items: list<TrackListItem>
      TrackListId: Option<string>
      CurrentItem: string
      CanAddCurrentItem: bool
      HideDone: bool
      CanShare: bool
    }


  type UpdatableItemProp =
    | IsDone of bool
    | Name of string

  type Msg =
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


    | Error of exn


  let init (listId: Option<string>) (canShare: bool) =
    {
      Items = []
      TrackListId = listId
      HideDone = false
      CurrentItem = ""
      CanAddCurrentItem = false
      CanShare = canShare
    },
    Cmd.ofMsg GetItems

  let update (msg: Msg) (state: State) (js: IJSRuntime) =
    let emptyListId =
      state, Cmd.ofMsg (Error(exn "ListId cannot be Empty"))

    match msg with
    | SetCurrentItem item ->
        { state with CurrentItem = item }, Cmd.ofMsg (ValidateItem(item))
    | GetItems ->
        match state.TrackListId with
        | Some listId ->
            state,
            Cmd.ofJS
              js
              "Mandadin.Database.GetListItems"
              [| listId; state.HideDone |]
              GetItemsSuccess
              Error
        | None -> emptyListId
    | GetItemsSuccess list ->
        { state with
            Items = list |> List.ofSeq
        },
        Cmd.none
    | ValidateItem item ->
        match state.TrackListId with
        | Some listid ->
            let onSuccess nameExists = ValidateItemSuccess(nameExists, item)
            state,
            Cmd.ofJS
              js
              "Mandadin.Database.ListItemExists"
              [| listid; state.CurrentItem |]
              onSuccess
              Error
        | None -> emptyListId
    | ValidateItemSuccess (nameExists, itemName) ->
        let canAdd = not nameExists && itemName.Length <> 0
        { state with
            CanAddCurrentItem = canAdd
        },
        Cmd.none

    | CreateItem item ->
        match state.TrackListId with
        | Some listid ->
            state,
            Cmd.ofJS
              js
              "Mandadin.Database.CreateListItem"
              [| listid; item |]
              CreateItemSuccess
              Error
        | None -> emptyListId
    | CreateItemSuccess item ->
        { state with
            Items = item :: state.Items
        },
        Cmd.none
    | UpdateItemProp (item, prop) ->
        match prop with
        | IsDone isDone ->
            state,
            Cmd.ofJS
              js
              "Mandadin.Database.UpdateListItem"
              [| { item with IsDone = isDone } |]
              UpdateItemPropSuccess
              Error
        | Name name ->
            state, Cmd.ofMsg (ValidateExisting { item with Name = name })
    | ValidateExisting item ->
        state,
        Cmd.ofJS js "Mandadin.Database.ListItemExists"
          [| item.ListId; item.Name |] (fun exists ->
          ValidateExistingSuccess(exists, item)) Error
    | ValidateExistingSuccess (exists, item) ->
        match exists with
        | true -> state, Cmd.none
        | false ->
            state,
            Cmd.ofJS
              js
              "Mandadin.Database.UpdateListItem"
              [| item |]
              UpdateItemPropSuccess
              Error
    | UpdateItemPropSuccess item ->
        let items =
          state.Items
          |> List.map (fun i -> if i.Id = item.Id then item else i)

        { state with Items = items }, Cmd.none
    | DeleteItem item ->
        state,
        Cmd.ofJS
          js
          "Mandadin.Database.DeleteListItem"
          [| item |]
          DeleteItemSuccess
          Error
    | DeleteItemSuccess item ->
        let items =
          state.Items
          |> List.filter (fun i -> i.Id <> item.Id)

        { state with Items = items }, Cmd.none
    | Error ex ->
        eprintfn "Update Error [%s]" ex.Message
        state, Cmd.none

  let private newItemForm (state: State) (dispatch: Dispatch<Msg>) =
    let currentContentTxt = "Nombre del objeto..."
    form [
           attr.``class`` "row flex-spaces background-muted border notes-form"
           on.submit (fun _ -> CreateItem state.CurrentItem |> dispatch)
         ] [
      fieldset [ attr.``class`` "form-group" ] [
        label [ attr.``for`` "current-content" ] [
          text currentContentTxt
        ]
        textarea [
                   attr.id "current-content"
                   attr.placeholder currentContentTxt
                   bind.input.string
                     state.CurrentItem
                     (SetCurrentItem >> dispatch)
                 ] []
      ]
      button [
               attr.``type`` "submit"
               attr.disabled (not state.CanAddCurrentItem)
             ] [
        Icon.Get Save None
      ]
    ]

  let private listItem (item: TrackListItem) (dispatch: Dispatch<Msg>) =
    li [
         attr.key item.Id
         attr.``class`` "listitem-item"
       ] [
      input [ attr.``type`` "checkbox"
              attr.``class`` "listitem-item-checkbox"
              attr.id item.Id
              bind.``checked`` item.IsDone (fun isDone ->
                UpdateItemProp(item, (IsDone isDone)) |> dispatch) ]
      input [ bind.input.string item.Name (fun name ->
                ValidateExisting { item with Name = name }
                |> dispatch) ]
      button [
               attr.``class`` "paper-btn btn-small btn-danger-outline m-0"
               on.click (fun _ -> DeleteItem item |> dispatch)
             ] [
        Icon.Get Trash None
      ]
    ]

  let view (state: State) (dispatch: Dispatch<Msg>) =
    article [] [
      newItemForm state dispatch
      ul [ attr.``class`` "tracklist-list" ] [
        for item in state.Items do
          listItem item dispatch
      ]
    ]


  type Page() =
    inherit ProgramComponent<State, Msg>()

    [<Parameter>]
    member val ListId: Option<string> = None with get, set

    [<Parameter>]
    member val CanShare: bool = false with get, set

    override this.Program =
      let init _ = init this.ListId this.CanShare
      let update msg state = update msg state this.JSRuntime
      Program.mkProgram init update view


[<RequireQualifiedAccess>]
module Lists =

  type State =
    {
      TrackLists: list<TrackList>
      CurrentListName: string
      CanAddCurrentName: bool
    }

  type Msg =
    | SetCurrentListName of string
    | RequestRoute of string

    | GetLists
    | GetListsSuccess of seq<TrackList>

    | ValidateListName of string
    | ValidateListNameSuccess of nameExists: bool * name: string

    | CreateList of string
    | CreateListSuccess of TrackList

    | DeleteList of TrackList
    | DeleteListSuccess of TrackList

    | Error of exn

  let init (_: 'arg) =
    {
      TrackLists = list.Empty
      CurrentListName = ""
      CanAddCurrentName = false
    },
    Cmd.ofMsg GetLists

  let update (msg: Msg)
             (state: State)
             (js: IJSRuntime)
             (onRouteRequested: Option<string -> unit>)
             =
    match msg with
    | SetCurrentListName name ->
        { state with CurrentListName = name }, Cmd.ofMsg (ValidateListName name)
    | RequestRoute listid ->
        match onRouteRequested with
        | Some onRouteRequested ->
            onRouteRequested listid
            state, Cmd.none
        | None -> state, Cmd.none
    | GetLists ->
        state,
        Cmd.ofJS js "Mandadin.Database.FindLists" [||] GetListsSuccess Error
    | GetListsSuccess items ->
        { state with
            TrackLists = items |> List.ofSeq
        },
        Cmd.none
    | ValidateListName name ->
        state,
        Cmd.ofJS js "Mandadin.Database.ListNameExists" [| name |] (fun exists ->
          ValidateListNameSuccess(exists, name)) Error
    | ValidateListNameSuccess (nameExists, name) ->
        if nameExists then
          { state with CanAddCurrentName = false },
          Cmd.ofMsg (Error(exn "Name already exists"))
        else
          { state with
              CanAddCurrentName = true && name.Length <> 0
          },
          Cmd.none
    | CreateList name ->
        state,
        Cmd.ofJS
          js
          "Mandadin.Database.CreateList"
          [| name |]
          CreateListSuccess
          Error
    | CreateListSuccess list ->
        { state with
            TrackLists = list :: state.TrackLists
            CurrentListName = ""
        },
        Cmd.none
    | DeleteList item ->
        state,
        Cmd.ofJS js "Mandadin.Database.DeleteList" [| item.Id; item.Rev |] (fun _ ->
          DeleteListSuccess item) Error
    | DeleteListSuccess item ->
        let list =
          state.TrackLists
          |> List.filter (fun i -> i <> item)

        { state with TrackLists = list }, Cmd.none
    | Error ex ->
        eprintfn "Update Error: [%s]" ex.Message
        state, Cmd.none

  let private newListForm (state: State) (dispatch: Dispatch<Msg>) =
    let submitBtnTxt = "Guardar"
    let currentContentTxt = "Nombre de la nueva lista..."
    form [
           attr.``class`` "row flex-spaces background-muted border notes-form"
           on.submit (fun _ -> CreateList state.CurrentListName |> dispatch)
         ] [
      fieldset [ attr.``class`` "form-group" ] [
        label [ attr.``for`` "current-content" ] [
          text currentContentTxt
        ]
        textarea [
                   attr.id "current-content"
                   attr.placeholder currentContentTxt
                   bind.input.string
                     state.CurrentListName
                     (SetCurrentListName >> dispatch)
                 ] []
      ]
      button [
               attr.``type`` "submit"
               attr.disabled (not state.CanAddCurrentName)
             ] [
        Icon.Get Save None
      ]
    ]

  let private listItem (item: TrackList) (dispatch: Dispatch<Msg>) =
    li [
         attr.``class`` "tracklist-item row flex-spaces"
       ] [
      button [
               attr.``class`` "paper-btn btn-small btn-danger-outline"
               on.click (fun _ -> DeleteList item |> dispatch)
             ] [
        Icon.Get Trash None
      ]
      p [ attr.``class`` "m-05" ] [
        text item.Id
      ]
      button [
               attr.``class`` "paper-btn btn-small btn-primary-outline"
               on.click (fun _ -> RequestRoute item.Id |> dispatch)
             ] [
        Icon.Get Text None
      ]
    ]

  let view (state: State) (dispatch: Dispatch<Msg>) =
    article [] [
      newListForm state dispatch
      ul [
           attr.``class`` "tracklist-list child-borders"
         ] [
        for item in state.TrackLists do
          listItem item dispatch
      ]
    ]


  type Page() as this =
    inherit ProgramComponent<State, Msg>()

    [<Parameter>]
    member val OnRouteRequested: Option<string -> unit> = None with get, set

    override _.Program =
      let update msg state =
        update msg state this.JSRuntime this.OnRouteRequested

      Program.mkProgram init update view
