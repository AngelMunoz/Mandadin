namespace Mandadin.Client.Components.TrackListItems

open System
open Microsoft.AspNetCore.Components

open Bolero
open Bolero.Html
open Mandadin.Client

type NewItemForm() =
  inherit Component()
  let mutable objectName = ""

  [<Parameter>]
  member val HideDone: bool = false with get, set

  [<Parameter>]
  member val OnSubmit: string -> unit = ignore with get, set

  [<Parameter>]
  member val OnHideDoneChange: bool -> unit = ignore with get, set

  override self.Render() =
    form {
      attr.``class`` "row flex-spaces background-muted border notes-form"
      on.submit (fun _ -> self.OnSubmit objectName)

      fieldset {
        attr.``class`` "form-group"

        label {
          attr.``for`` "current-content"
          text "Nombre del objeto..."
        }

        textarea {
          attr.id "current-content"
          attr.placeholder objectName

          bind.input.string objectName (fun name -> objectName <- name)
        }

        label {
          attr.``for`` "paperCheck1"
          attr.``class`` "paper-check"

          input {
            attr.id "paperCheck1"
            attr.name "paperChecks"
            attr.``type`` "checkbox"

            bind.``checked`` self.HideDone (fun hideDone ->
              self.OnHideDoneChange hideDone)
          }

          span { text "Esconder Terminados" }
        }
      }

      button {
        attr.``type`` "submit"
        attr.``class`` "paper-btn btn-small"
        attr.disabled (String.IsNullOrWhiteSpace objectName)
        Icon.Get Save
      }
    }


[<RequireQualifiedAccess>]
module TrackListItems =

  let Stringify (items: list<TrackListItem>) : string =
    let isDoneToX (isDone: bool) = if isDone then 'x' else ' '

    let stringified =
      items
      |> List.map (fun item ->
        sprintf "[ %c ] %s" (isDoneToX item.IsDone) item.Name)

    System.String.Join('\n', stringified)

type ToolbarState<'State
  when 'State: (member TrackListId: string)
  and 'State: (member CanShare: bool)
  and 'State: (member Items: list<TrackListItem>)> = 'State


type TrackListComponents =

  static member inline Toolbar<'State when ToolbarState<'State>>
    (
      state: 'State,
      share: IShareService,
      ?onBackRequested: unit -> unit
    ) =
    let onBackRequested =
      defaultArg onBackRequested ignore

    div {
      attr.``class`` "border"

      section {
        attr.``class`` "row flex-center"
        h4 { text state.TrackListId }
      }

      section {
        attr.``class`` "row flex-center"

        button {
          attr.``class`` "paper-btn btn-small"
          on.click (fun _ -> onBackRequested ())
          Icon.Get Back
        }

        cond state.CanShare
        <| function
          | true ->
            button {
              attr.``class`` "paper-btn btn-small"

              on.click (fun _ ->
                let content =
                  state.Items |> TrackListItems.Stringify

                share.Share(state.TrackListId, content) |> ignore)

              Icon.Get Share
            }
          | false -> empty ()

        button {
          attr.``class`` "paper-btn btn-small"

          on.click (fun _ ->
            let content =
              state.Items |> TrackListItems.Stringify

            share.ToClipboard content |> ignore)

          Icon.Get Copy
        }
      }
    }
