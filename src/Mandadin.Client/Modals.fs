namespace Mandadin.Client

open Bolero.Html
open Microsoft.AspNetCore.Components

[<RequireQualifiedAccess>]
module Modals =
  open Bolero

  module Import =
    open Elmish

    [<Struct>]
    type ImportData = { title: string; content: string }

    [<Struct>]
    type Msg =
      | SetTitle of title: string
      | SetContent of content: string
      | Import
      | Dismiss

    type ImportTrackList() as this =
      inherit ProgramComponent<ImportData, Msg>()

      let update msg importdata =
        match msg with
        | SetTitle title -> { importdata with title = title }
        | SetContent content -> { importdata with content = content }
        | Import ->
          this.OnImport importdata
          importdata
        | Dismiss ->
          this.OnDismiss()
          importdata

      let view (importData) dispatch =
        section {
          input {
            attr.``class`` "modal-state"
            attr.id "modal-1"
            attr.``type`` "checkbox"
            attr.``checked`` true
          }

          div {
            attr.``class`` "modal"

            label { attr.``class`` "modal-bg" }

            div {
              attr.``class`` "modal-body"

              label {
                attr.``class`` "btn-close"
                on.click (fun _ -> dispatch Dismiss)
                Icon.Get Close
              }

              h4 {
                attr.``class`` "modal-title"
                text "Titulo de la nueva lista"
              }

              input {
                bind.input.string importData.title (fun txt ->
                  dispatch (SetTitle txt))
              }

              h5 {
                attr.``class`` "modal-subtitle"
                text """Pega el texto proveniente de otros "Mandadin" abajo"""
                br

                text
                  "NOTA: SI EL NOMBRE ES IGUAL A UNO EXISTENTE SE BORRARA EL CONTENIDO ANTERIOR"
              }

              textarea {
                attr.``class`` "modal-text"
                attr.rows 8
                attr.cols 32

                bind.input.string importData.content (fun txt ->
                  dispatch (SetContent txt))
              }

              button {
                attr.``class`` "paper-btn btn-info-outline"

                on.click (fun _ -> dispatch Import)

                Icon.Get Check
                text "Importar contenido"
              }

              button {
                attr.``class`` "paper-btn btn-danger-outline"
                on.click (fun _ -> dispatch Dismiss)
                Icon.Get Trash
                text "Cancelar"
              }
            }
          }
        }


      [<Parameter>]
      member val IsOpen = false with get, set

      [<Parameter>]
      member val ImportData = { title = ""; content = "" } with get, set

      [<Parameter>]
      member val OnDismiss: (unit -> unit) = id with get, set

      [<Parameter>]
      member val OnImport: (ImportData -> unit) = ignore with get, set

      override this.Program =
        Program.mkSimple (fun _ -> this.ImportData) update view


  let DeleteResourceModal
    (content: string * string * string)
    (isOpen: bool)
    (action: Result<bool, unit> -> unit)
    =
    let (title, subtitle, message) = content

    section {
      input {
        attr.``class`` "modal-state"
        attr.id "modal-1"
        attr.``type`` "checkbox"
        attr.``checked`` isOpen
      }

      div {
        attr.``class`` "modal"
        label { attr.``class`` "modal-bg" }

        div {
          attr.``class`` "modal-body"

          label {
            attr.``class`` "btn-close"
            on.click (fun _ -> Error() |> action)
            Icon.Get Close
          }

          h4 {
            attr.``class`` "modal-title"
            text title
          }

          h5 {
            attr.``class`` "modal-subtitle"
            text subtitle
          }

          p {
            attr.``class`` "modal-text"
            text message
          }

          button {
            attr.``class`` "paper-btn btn-danger-outline"
            on.click (fun _ -> Ok true |> action)
            Icon.Get Check
            text "Si, Continuar"
          }

          button {
            attr.``class`` "paper-btn btn-success-outline"
            on.click (fun _ -> Ok false |> action)
            Icon.Get Trash
            text "Cancelar"
          }
        }
      }
    }
