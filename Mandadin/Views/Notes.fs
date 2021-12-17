namespace Mandadin.Views


open Bolero

open Fun.Blazor

open FSharp.Control.Reactive
open FSharp.Data.Adaptive

open Mandadin

[<RequireQualifiedAccess>]
module Notes =

  let private noteform
    (createNote: string -> Async<unit>)
    (clipboard: IClipboardService)
    =
    adaptiview () {
      let! (contentValue, setContentValue) = cval("").WithSetter()

      form () {
        class' "row flex-spaces background-muted border notes-form"

        onsubmitAsync (fun _ -> createNote contentValue)

        childContent [
          fieldset () {
            class' "form-group"

            childContent [
              Html.label [ Html.attr.``for`` "current-content" ] [
                Html.text "Escribe algo..."
              ]
              textarea () {
                placeholder "Escribe algo..."
                value contentValue
                oninput (fun event -> setContentValue (unbox event.Value))
              }
            ]
          }
          button () {
            type' "submit"
            disabled (contentValue.Length = 0)
            childContent (Icon.Get Save None)
          }
          button () {
            type' "button"

            onclickAsync (fun _ ->
              async {
                let! content = clipboard.GetFromClipboard() |> Async.AwaitTask
                setContentValue content
              })

            childContent (Icon.Get Clipboard None)
          }
        ]
      }
    }

  let private noteItem
    (deleteNote: Note -> Async<unit>)
    (updateNote: Note -> Async<Note>)
    (clipboard: IClipboardService)
    (share: IShareService)
    (canShare: bool)
    (item: Note)
    =
    adaptiview () {
      let! item, setItem = cval(item).WithSetter()

      li () {
        class' "note-list-item m-05"

        childContent [
          textarea () {
            value item.Content

            oninputAsync (fun event ->
              async {
                let! note = updateNote { item with Content = unbox event.Value }
                setItem note
              })
          }
          section () {
            class' "row"

            childContent [
              button () {
                class' "paper-btn btn-small btn-muted-outline"

                onclickAsync (fun _ ->
                  async {
                    do!
                      clipboard.SendToClipboard item.Content
                      |> Async.AwaitTask
                      |> Async.Ignore
                  })

                childContent (Icon.Get Copy None)
              }
              if canShare then
                button () {
                  class' "paper-btn btn-small btn-muted-outline"

                  onclickAsync (fun _ ->
                    async {
                      do!
                        share.ShareContent("Mandadin", item.Content, None)
                        |> Async.AwaitTask
                        |> Async.Ignore
                    })

                  childContent (Icon.Get Share None)
                }
              button () {
                class' "paper-btn btn-small btn-danger-outline"
                onclickAsync (fun _ -> deleteNote item)
                childContent (Icon.Get Trash None)
              }
            ]
          }
        ]
      }
    }

  let View (canShare: bool) =
    let _view
      (
        hook: IComponentHook,
        notes: INoteService,
        clipboard: IClipboardService,
        share: IShareService
      ) =

      let noteList = hook.UseStore [||]

      hook.OnInitialized
      |> Observable.map notes.FindNotes
      |> Observable.switchTask
      |> Observable.subscribe (Array.ofList >> noteList.Publish)
      |> hook.AddDispose

      let createNote (content: string) =
        async {
          if System.String.IsNullOrWhiteSpace content |> not then
            do!
              notes.CreateNote(content)
              |> Async.AwaitTask
              |> Async.Ignore

            let! notes = notes.FindNotes() |> Async.AwaitTask
            noteList.Publish(notes |> Array.ofList)
        }

      let deleteNote (note: Note) =
        async {
          do!
            notes.DeleteNote(note.Id, note.Rev)
            |> Async.AwaitTask
            |> Async.Ignore

          let! notes = notes.FindNotes() |> Async.AwaitTask
          noteList.Publish(notes |> Array.ofList)
        }

      let updateNote (note: Note) =
        notes.UpdateNote note |> Async.AwaitTask

      let noteListTpl =
        adaptiview () {
          let! notelist = hook.UseCVal noteList

          Virtualize'() {
            Items notelist

            ChildContent(
              noteItem deleteNote updateNote clipboard share canShare
            )
          }

        }

      article () {
        childContent [
          html.inject ("note-form", noteform createNote)

          ul () {
            class' "notes-list"
            childContent noteListTpl
          }
        ]
      }

    html.inject ("mandadin-notes", _view)
