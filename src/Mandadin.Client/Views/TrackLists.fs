namespace Mandadin.Client.Views

open Elmish
open Microsoft.JSInterop
open Bolero
open Bolero.Html
open Bolero.Remoting.Client
open Mandadin.Client
open Microsoft.AspNetCore.Components

[<RequireQualifiedAccess>]
module Lists =

  let private parseContentString (content: string): array<array<obj>> =
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

  type State =
    {
      TrackLists: list<TrackList>
      CurrentListName: string
      CanAddCurrentName: bool
      ShowConfirmDeleteModal: bool
      ShowImportDialog: bool
      FromClipboard: Option<string>
    }

  type Msg =
    | SetCurrentListName of string
    | RequestRoute of string

    | GetLists
    | GetListsSuccess of seq<TrackList>

    | ValidateListName of string
    | ValidateListNameSuccess of nameExists: bool * name: string

    | CreateList of string
    | CreateFromImport of string * array<array<obj>>
    | CreateListSuccess of TrackList

    | DeleteList of TrackList
    | DeleteListSuccess of TrackList

    | ShowConfirmDeleteModal of bool
    | ShowConfirmDeleteModalAction of TrackList * Result<bool, unit>

    | ShowImportDialog of bool
    | ShowImportDialogAction of Result<string * string, unit>

    | FromClipboard
    | FromClipboardSuccess of string

    | Error of exn

  let init (_: 'arg) =
    {
      TrackLists = list.Empty
      CurrentListName = ""
      CanAddCurrentName = false
      ShowConfirmDeleteModal = false
      ShowImportDialog = false
      FromClipboard = None
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
            TrackLists =
              (list :: state.TrackLists)
              |> List.sortBy (fun item -> item.Id)
            CurrentListName = ""
            ShowImportDialog = false
        },
        Cmd.none
    | FromClipboard ->
        state,
        Cmd.ofJS
          js
          "Mandadin.Clipboard.ReadTextFromClipboard"
          [||]
          FromClipboardSuccess
          Error
    | FromClipboardSuccess content ->
        { state with
            FromClipboard = Some content
        },
        Cmd.ofMsg (ShowImportDialog true)
    | ShowImportDialog show -> { state with ShowImportDialog = show }, Cmd.none
    | ShowImportDialogAction result ->
        let cmd =
          match result with
          | Ok (title, content) ->
              let parsed = parseContentString content
              Cmd.ofMsg (CreateFromImport(title, parsed))
          | _ -> Cmd.ofMsg (ShowImportDialog false)

        { state with FromClipboard = None }, cmd
    | CreateFromImport (title, items) ->
        state,
        Cmd.ofJS
          js
          "Mandadin.Database.ImportList"
          [| title; items |]
          CreateListSuccess
          Error
    | ShowConfirmDeleteModal show ->
        { state with
            ShowConfirmDeleteModal = show
        },
        Cmd.none
    | ShowConfirmDeleteModalAction (item, result) ->
        let cmd =
          match result with
          | Ok result when result -> Cmd.ofMsg (DeleteList item)
          | _ -> Cmd.ofMsg (ShowConfirmDeleteModal false)

        state, cmd
    | DeleteList item ->
        state,
        Cmd.ofJS js "Mandadin.Database.DeleteList" [| item.Id; item.Rev |] (fun _ ->
          DeleteListSuccess item) Error
    | DeleteListSuccess item ->
        let list =
          state.TrackLists
          |> List.filter (fun i -> i <> item)

        { state with
            TrackLists = list
            ShowConfirmDeleteModal = false
        },
        Cmd.none
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
      button [
               attr.``class`` "paper-btn btn-small"
               attr.``type`` "button"
               on.click (fun _ -> FromClipboard |> dispatch)
             ] [
        Icon.Get Import None
      ]
    ]

  let private listItem (item: TrackList) (dispatch: Dispatch<Msg>) =
    li [
         attr.``class`` "tracklist-item row flex-spaces"
         attr.key item.Id
       ] [
      p [ attr.``class`` "m-05" ] [
        text item.Id
      ]
      button [
               attr.``class`` "paper-btn btn-small btn-primary-outline"
               on.click (fun _ -> RequestRoute item.Id |> dispatch)
             ] [
        Icon.Get Text None
      ]
      button [
               attr.``class`` "paper-btn btn-small btn-danger-outline"
               on.click (fun _ -> ShowConfirmDeleteModal true |> dispatch)
             ] [
        Icon.Get Trash None
      ]
    ]

  let view (state: State) (dispatch: Dispatch<Msg>) =
    let deleteModal (item: TrackList) =
      let title = "Borrar Elemento"
      let subtitle = "esta operacion es irreversible"

      let txt =
        sprintf """Proceder con el borrado de "%s"?""" item.Id

      Modals.DeleteResourceModal (title, subtitle, txt)
        state.ShowConfirmDeleteModal (fun result ->
        ShowConfirmDeleteModalAction(item, result)
        |> dispatch)

    article [] [
      Modals.ImportTrackList
        state.ShowImportDialog
        (ShowImportDialogAction >> dispatch)
        state.FromClipboard
      newListForm state dispatch
      ul [
           attr.``class`` "tracklist-list child-borders"
         ] [
        for item in state.TrackLists do
          deleteModal item
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
#if DEBUG
      |> Program.withConsoleTrace
#endif
