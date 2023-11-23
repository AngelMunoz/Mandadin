namespace Mandadin.Client.Views

open Elmish
open Microsoft.JSInterop
open Bolero
open Bolero.Html
open Bolero.Remoting.Client
open Mandadin.Client
open Microsoft.AspNetCore.Components

[<RequireQualifiedAccess>]
module Notes =

  type State =
    { CurrentContent: string
      Notes: list<Note>
      CanShare: bool }

  type Msg =
    | SetCurrentContent of string

    | DeleteNote of Id: string * Rev: string
    | DeleteNoteSuccess of array<string>

    | GetNotes
    | GetNotesSuccess of seq<Note>

    | CreateNote of string
    | CreateNoteSuccess of Note

    | UpdateNote of Note
    | UpdateNoteSuccess of Note

    (* Web API Cases*)
    | FromClipboard
    | FromClipboardSuccess of string

    | ToClipboard of Note
    | ToClipboardSuccess

    | ShareContent of Note
    | ShareContentSuccess

    (* Any error will land here *)
    | Error of exn

  let private init (canShare: bool) =
    { CurrentContent = ""
      Notes = list.Empty
      CanShare = canShare },
    Cmd.ofMsg GetNotes

  let private update (msg: Msg) (state: State) (js: IJSRuntime) =
    match msg with
    | SetCurrentContent content ->
      { state with CurrentContent = content }, Cmd.none
    | DeleteNote(noteid, rev) ->
      state,
      Cmd.OfJS.either
        js
        "Mandadin.Database.DeleteNote"
        [| noteid; rev |]
        DeleteNoteSuccess
        Error
    | DeleteNoteSuccess arr ->
      let noteid = Array.head arr

      let notes =
        state.Notes
        |> List.filter (fun note -> note.Id <> noteid)

      { state with Notes = notes }, Cmd.none
    | GetNotes ->
      state,
      Cmd.OfJS.either
        js
        "Mandadin.Database.FindNotes"
        [||]
        GetNotesSuccess
        Error
    | GetNotesSuccess notes ->
      { state with
          Notes = notes |> List.ofSeq },
      Cmd.none
    | CreateNote content ->
      state,
      Cmd.OfJS.either
        js
        "Mandadin.Database.CreateNote"
        [| content |]
        CreateNoteSuccess
        Error
    | CreateNoteSuccess note ->
      { state with
          Notes = note :: state.Notes },
      Cmd.none
    | UpdateNote note ->
      state,
      Cmd.OfJS.either
        js
        "Mandadin.Database.UpdateNote"
        [| note |]
        UpdateNoteSuccess
        Error
    | UpdateNoteSuccess updated ->
      let notes =
        state.Notes
        |> List.map (fun note ->
          if note.Id = updated.Id then
            updated
          else
            note)

      { state with
          Notes = notes
          CurrentContent = "" },
      Cmd.none
    | FromClipboard ->
      state,
      Cmd.OfJS.either
        js
        "Mandadin.Clipboard.ReadTextFromClipboard"
        [||]
        FromClipboardSuccess
        Error
    | FromClipboardSuccess content ->
      { state with CurrentContent = content }, Cmd.none
    | ToClipboard note ->
      let text =
        sprintf "Nota Mandadin:\n%s " note.Content

      state,
      Cmd.OfJS.either
        js
        "Mandadin.Clipboard.CopyTextToClipboard"
        [| text |]
        (fun _ -> ToClipboardSuccess)
        Error
    | ToClipboardSuccess -> state, Cmd.none
    | ShareContent note ->
      let title = "Nota"
      let text = sprintf "%s" note.Content

      state,
      Cmd.OfJS.either
        js
        "Mandadin.Share.ShareContent"
        [| title; text; "" |]
        (fun _ -> ShareContentSuccess)
        Error
    | ShareContentSuccess -> state, Cmd.none
    | Error err ->
      eprintfn "%s" err.Message
      state, Cmd.none

  let private newNoteForm (state: State) (dispatch: Dispatch<Msg>) =
    let submitBtnTxt = "Guardar"
    let currentContentTxt = "Escribe algo..."

    form {
      attr.``class`` "row flex-spaces background-muted border notes-form"
      on.submit (fun _ -> CreateNote(state.CurrentContent) |> dispatch)

      fieldset {
        attr.``class`` "form-group"

        label {
          attr.``for`` "current-content"
          text currentContentTxt
        }

        textarea {
          attr.id "current-content"
          attr.placeholder currentContentTxt
          bind.input.string state.CurrentContent (SetCurrentContent >> dispatch)
        }
      }

      button {
        attr.``type`` "submit"
        attr.disabled (state.CurrentContent.Length = 0)
        text submitBtnTxt
      }

      button {
        attr.``type`` "button"
        on.click (fun _ -> FromClipboard |> dispatch)
        Icon.Get Clipboard
      }
    }

  let private noteItem (item: Note) (canShare: bool) (dispatch: Dispatch<Msg>) =
    li {
      attr.``class`` "note-list-item m-05"

      textarea {
        bind.input.string item.Content (fun text ->
          dispatch (UpdateNote { item with Content = text }))

        section {
          attr.``class`` "row"

          button {
            attr.``class`` "paper-btn btn-small btn-muted-outline"
            on.click (fun _ -> ToClipboard item |> dispatch)
            Icon.Get Copy
          }

          cond canShare
          <| function
            | false -> empty ()
            | true ->
              button {
                attr.``class`` "paper-btn btn-small btn-muted-outline"
                on.click (fun _ -> ShareContent item |> dispatch)
                Icon.Get Share
              }

          button {
            attr.``class`` "paper-btn btn-small btn-danger-outline"
            on.click (fun _ -> DeleteNote(item.Id, item.Rev) |> dispatch)
            Icon.Get Trash
          }
        }
      }
    }

  let view (state: State) (dispatch: Dispatch<Msg>) =
    article {
      newNoteForm state dispatch

      ul {
        attr.``class`` "notes-list"

        virtualize.comp {
          virtualize.placeholder (fun _ -> div { text "Cargando..." })
          let! item = virtualize.items state.Notes
          noteItem item state.CanShare dispatch
        }
      }
    }


  type Page() as this =
    inherit ProgramComponent<State, Msg>()

    [<Parameter>]
    member val CanShare: bool = false with get, set

    override _.Program =
      let init (_: 'arg) = init this.CanShare
      let update msg state = update msg state this.JSRuntime
      Program.mkProgram init update view
