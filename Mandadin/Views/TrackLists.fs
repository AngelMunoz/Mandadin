namespace Mandadin.Views

open Elmish
open Fun.Blazor
open Fun.Blazor.elt
open Microsoft.JSInterop
open Bolero
open Bolero.Remoting.Client
open Mandadin
open Microsoft.AspNetCore.Components

[<RequireQualifiedAccess>]
module Lists =

  type State =
    { TrackLists: list<TrackList>
      ShowConfirmDeleteModal: Option<TrackList> }

  type Msg =
    | RequestRoute of string

    | GetLists
    | GetListsSuccess of seq<TrackList>

    | DeleteList of TrackList
    | DeleteListSuccess of TrackList

    | ShowConfirmDeleteModal of Option<TrackList>
    | ShowConfirmDeleteModalAction of TrackList * Result<bool, unit>

    | Error of exn

  let init (_: 'arg) =
    { TrackLists = list.Empty
      ShowConfirmDeleteModal = None },
    Cmd.ofMsg GetLists

  let update
    (msg: Msg)
    (state: State)
    (js: IJSRuntime)
    (onRouteRequested: Option<string -> unit>)
    =
    match msg with
    | RequestRoute listid ->
      match onRouteRequested with
      | Some onRouteRequested ->
        onRouteRequested listid
        state, Cmd.none
      | None -> state, Cmd.none
    | GetLists ->
      state,
      Cmd.OfJS.either
        js
        "Mandadin.Database.FindLists"
        [||]
        GetListsSuccess
        Error
    | GetListsSuccess items ->
      { state with
          TrackLists = items |> List.ofSeq },
      Cmd.none
    | ShowConfirmDeleteModal show ->
      { state with
          ShowConfirmDeleteModal = show },
      Cmd.none
    | ShowConfirmDeleteModalAction (item, result) ->
      let cmd =
        match result with
        | Ok result when result -> Cmd.ofMsg (DeleteList item)
        | _ -> Cmd.ofMsg (ShowConfirmDeleteModal None)

      state, cmd
    | DeleteList item ->
      state,
      Cmd.OfJS.either
        js
        "Mandadin.Database.DeleteList"
        [| item.Id; item.Rev |]
        (fun _ -> DeleteListSuccess item)
        Error
    | DeleteListSuccess item ->
      let list =
        state.TrackLists
        |> List.filter (fun i -> i <> item)

      { state with
          TrackLists = list
          ShowConfirmDeleteModal = None },
      Cmd.none
    | Error ex ->
      eprintfn "Update Error: [%s]" ex.Message
      state, Cmd.none

  let private trackListForm () =

    let _form
      (
        hook: IComponentHook,
        tracklists: ITrackListService,
        clipboard: IClipboardService,
        nav: NavigationManager
      ) =
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
          let value: string = unbox event.Value
          store.Publish value
          printfn $"{value}"
          match! (tracklists.ListNameExists value)
                 |> Async.AwaitTask with
          | true ->
            printfn "false" 
            isDisabled.Publish true
          | false ->
            printfn "true" 
            isDisabled.Publish false
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

    html.inject ("tracklist-form", _form)

  let private listItem (item: TrackList) (dispatch: Dispatch<Msg>) =
    Html.li [ Html.attr.``class`` "tracklist-item row flex-spaces"
              Html.attr.key item.Id ] [
      Html.p [ Html.attr.``class`` "m-05" ] [
        Html.text item.Id
      ]
      Html.button [ Html.attr.``class``
                      "paper-btn btn-small btn-primary-outline"
                    Html.on.click (fun _ -> RequestRoute item.Id |> dispatch) ] [
        Icon.Get Text None
      ]
      Html.button [ Html.attr.``class`` "paper-btn btn-small btn-danger-outline"
                    Html.on.click
                      (fun _ -> ShowConfirmDeleteModal(Some item) |> dispatch) ] [
        Icon.Get Trash None
      ]
    ]

  let view (state: State) (dispatch: Dispatch<Msg>) =
    let deleteModal (item: TrackList) =
      let title = "Borrar Elemento"
      let subtitle = "esta operacion es irreversible"

      let txt =
        sprintf """Proceder con el borrado de "%s"?""" item.Id

      let showModal = state.ShowConfirmDeleteModal.IsSome


      Modals.DeleteResourceModal
        (title, subtitle, txt)
        showModal
        (fun result ->
          ShowConfirmDeleteModalAction(item, result)
          |> dispatch)

    Html.article [] [
      trackListForm ()
      if state.ShowConfirmDeleteModal.IsSome then
        deleteModal state.ShowConfirmDeleteModal.Value
      Html.ul [ Html.attr.``class`` "tracklist-list child-borders" ] [
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
#if DEBUG
      |> Program.withConsoleTrace
#endif
