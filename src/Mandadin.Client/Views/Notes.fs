namespace Mandadin.Client.Views.Notes

open System

open Microsoft.JSInterop
open Microsoft.AspNetCore.Components

open IcedTasks

open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting.Client

open Mandadin.Client

type NewNoteForm() =
  inherit Component()

  let mutable noteContent: string = ""

  [<Parameter>]
  member val OnNewNote: (Note -> unit) = ignore with get, set

  [<Inject>]
  member val NoteService: INoteService =
    Unchecked.defaultof<INoteService> with get, set

  [<Inject>]
  member val Share: IShareService =
    Unchecked.defaultof<IShareService> with get, set

  override self.Render() : Node =
    form {
      attr.``class`` "row flex-spaces background-muted border notes-form"

      on.task.submit (fun _ ->
        taskUnit {
          let! created = self.NoteService.CreateNote noteContent

          match created with
          | ValueSome note ->
            self.OnNewNote note
            noteContent <- ""
          | ValueNone -> ()
        })

      fieldset {
        attr.``class`` "form-group"

        label {
          attr.``for`` "current-content"
          text "Escribe algo..."
        }

        textarea {
          attr.id "current-content"
          attr.placeholder "Escribe algo..."
          bind.input.string noteContent (fun text -> noteContent <- text)
        }
      }

      button {
        attr.``type`` "submit"
        attr.disabled (String.IsNullOrWhiteSpace noteContent)
        text "Guardar"
      }

      button {
        attr.``type`` "button"

        on.task.click (fun _ ->
          taskUnit {
            let! content = self.Share.FromClipboard()
            noteContent <- content
          })

        Icon.Get Clipboard
      }
    }


type NoteItem() =
  inherit Component()

  let textAreaRef = HtmlRef()

  member private self.onTextAreaBlur _ =
    taskUnit {
      match textAreaRef.Value with
      | Some ref ->
        let! content =
          self.jsRuntime.InvokeAsync<string>("Mandadin.Elements.GetValue", ref)

        if
          content <> self.Note.Content
          && not (String.IsNullOrWhiteSpace content)
        then
          self.OnNoteChanged(content, self.Note)
      | None -> ()

      return ()
    }

  [<Parameter>]
  member val OnNoteChanged: (string * Note -> unit) = ignore with get, set

  [<Parameter>]
  member val OnNoteDeleted: (Note -> unit) = ignore with get, set

  [<Parameter>]
  member val Note: Note = Unchecked.defaultof<Note> with get, set

  [<Parameter>]
  member val CanShare: bool = false with get, set

  [<Inject>]
  member val NoteService: INoteService =
    Unchecked.defaultof<INoteService> with get, set

  [<Inject>]
  member val Share: IShareService =
    Unchecked.defaultof<IShareService> with get, set

  [<Inject>]
  member val jsRuntime: IJSRuntime =
    Unchecked.defaultof<IJSRuntime> with get, set

  override self.Render() : Node =

    li {
      attr.``class`` "note-list-item m-05"

      textarea {
        attr.value self.Note.Content

        on.task.blur self.onTextAreaBlur

        textAreaRef
      }

      section {
        attr.``class`` "row"

        button {
          attr.``class`` "paper-btn btn-small btn-muted-outline"

          on.task.click (fun _ ->
            taskUnit { do! self.Share.ToClipboard self.Note.Content })

          Icon.Get Copy
        }

        cond self.CanShare
        <| function
          | false -> empty ()
          | true ->
            button {
              attr.``class`` "paper-btn btn-small btn-muted-outline"

              on.task.click (fun _ ->
                taskUnit { do! self.Share.ShareNote self.Note.Content })

              Icon.Get Share
            }

        button {
          attr.``class`` "paper-btn btn-small btn-danger-outline"
          on.click (fun _ -> self.OnNoteDeleted self.Note)
          Icon.Get Trash
        }
      }
    }


type Page() =
  inherit Component()
  let mutable notes: list<Note> = list.Empty
  let mutable isSaving = false

  member self.Notes
    with get () = notes
    and set v =
      notes <- v
      self.StateHasChanged()

  [<Parameter>]
  member val CanShare: bool = false with get, set

  [<Inject>]
  member val NoteService: INoteService =
    Unchecked.defaultof<INoteService> with get, set

  member private self.onDeleteNote note =
    valueTaskUnit {
      do! self.NoteService.DeleteNote note

      self.Notes <-
        self.Notes
        |> List.filter (fun n -> n.Id <> note.Id)
    }
    |> ignore

  member private self.onNoteChanged(content, note) =
    valueTaskUnit {
      if isSaving then
        return ()

      isSaving <- true
      let! note = self.NoteService.UpdateNote(content, note)

      match note with
      | ValueSome note ->
        self.Notes <-
          self.Notes
          |> List.map (fun n -> if n.Id = note.Id then note else n)
      | ValueNone -> ()

      isSaving <- false
    }
    |> ignore

  override self.OnInitializedAsync() =
    taskUnit {
      let! foundNotes = self.NoteService.GetNotes()
      self.Notes <- foundNotes
    }

  override self.Render() : Node =
    article {
      comp<NewNoteForm> {
        "OnNewNote"
        => (fun note -> self.Notes <- note :: self.Notes)
      }

      ul {
        attr.``class`` "notes-list"

        virtualize.comp {
          virtualize.placeholder (fun _ -> div { text "Cargando..." })
          let! item = virtualize.items self.Notes

          comp<NoteItem> {
            "Note" => item
            "CanShare" => true
            "OnNoteChanged" => self.onNoteChanged

            "OnNoteDeleted" => self.onDeleteNote
          }
        }
      }
    }
