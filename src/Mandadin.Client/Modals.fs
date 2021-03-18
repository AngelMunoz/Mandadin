namespace Mandadin.Client

open Bolero.Html

[<RequireQualifiedAccess>]
module Modals =


  let DeleteResourceModal
    (content: string * string * string)
    (isOpen: bool)
    (action: Result<bool, unit> -> unit)
    =
    let (title, subtitle, message) = content

    section [] [
      input [ attr.``class`` "modal-state"
              attr.id "modal-1"
              attr.``type`` "checkbox"
              attr.``checked`` isOpen ]
      div [ attr.``class`` "modal" ] [
        label [ attr.``class`` "modal-bg" ] []
        div [ attr.``class`` "modal-body" ] [
          label [ attr.``class`` "btn-close"
                  on.click (fun _ -> Error() |> action) ] [
            Icon.Get Close None
          ]
          h4 [ attr.``class`` "modal-title" ] [
            text title
          ]
          h5 [ attr.``class`` "modal-subtitle" ] [
            text subtitle
          ]
          p [ attr.``class`` "modal-text" ] [
            text message
          ]
          button [ attr.``class`` "paper-btn btn-danger-outline"
                   on.click (fun _ -> Ok true |> action) ] [
            Icon.Get Check None
            text "Si, Continuar"
          ]
          button [ attr.``class`` "paper-btn btn-success-outline"
                   on.click (fun _ -> Ok false |> action) ] [
            Icon.Get Trash None
            text "Cancelar"
          ]
        ]
      ]
    ]

  let ImportTrackList
    (isOpen: bool)
    (action: Result<string * string, unit> -> unit)
    (title: Option<string>)
    (content: Option<string>)
    =
    let mutable title = defaultArg title ""
    let mutable content = defaultArg content ""

    section [] [
      input [ attr.``class`` "modal-state"
              attr.id "modal-1"
              attr.``type`` "checkbox"
              attr.``checked`` isOpen ]
      div [ attr.``class`` "modal" ] [
        label [ attr.``class`` "modal-bg" ] []
        div [ attr.``class`` "modal-body" ] [
          label [ attr.``class`` "btn-close"
                  on.click (fun _ -> Error() |> action) ] [
            Icon.Get Close None
          ]
          h4 [ attr.``class`` "modal-title" ] [
            text "Titulo de la nueva lista"
          ]
          input [ bind.input.string title (fun txt -> title <- txt) ]
          h5 [ attr.``class`` "modal-subtitle" ] [
            text """Pega el texto proveniente de otros "Mandadin" abajo"""
            br []
            text
              "NOTA: SI EL NOMBRE ES IGUAL A UNO EXISTENTE SE BORRARA EL CONTENIDO ANTERIOR"
          ]
          textarea [ attr.``class`` "modal-text"
                     attr.rows 6
                     bind.input.string content (fun txt -> content <- txt) ] []
          button [ attr.``class`` "paper-btn btn-info-outline"
                   on.click (fun _ -> Ok(title, content) |> action) ] [
            Icon.Get Check None
            text "Importar contenido"
          ]
          button [ attr.``class`` "paper-btn btn-danger-outline"
                   on.click (fun _ -> Error() |> action) ] [
            Icon.Get Trash None
            text "Cancelar"
          ]
        ]
      ]
    ]
