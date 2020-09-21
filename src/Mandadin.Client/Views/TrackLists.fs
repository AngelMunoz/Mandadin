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
      HideDone: bool
      CanShare: bool
    }

  type Msg =
    | GetItems
    | GetItemsSuccess of seq<TrackListItem>

    | Error of exn


  let init (listId: Option<string>) (canShare: bool) =
    {
      Items = []
      TrackListId = listId
      HideDone = false
      CanShare = canShare
    },
    Cmd.ofMsg GetItems

  let update (msg: Msg) (state: State) (js: IJSRuntime) =
    match msg with
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
        | None -> state, Cmd.ofMsg (Error(exn "ListId cannot be Empty"))
    | GetItemsSuccess list ->
        { state with
            Items = list |> List.ofSeq
        },
        Cmd.none
    | Error ex ->
        eprintfn "Update Error [%s]" ex.Message
        state, Cmd.none

  let view (state: State) (dispatch: Dispatch<Msg>) = Html.article [] []


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

  let listItem (item: TrackList) (dispatch: Dispatch<Msg>) =
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
