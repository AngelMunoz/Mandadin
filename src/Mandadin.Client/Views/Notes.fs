namespace Mandadin.Client.Views

open Elmish
open Microsoft.JSInterop
open Bolero
open Bolero.Html
open Bolero.Remoting.Client
open Mandadin.Client

[<RequireQualifiedAccess>]
module Notes =

  type State =
    {
      CurrentContent: string
      Notes: list<Note>
    }

  type Msg =
    | ValidateContent
    | SetNotes of list<Note>
    | SetCurrentContent of string
    | Error of exn

  let init (_: 'arg) =
    {
      CurrentContent = ""
      Notes = list.Empty
    },
    Cmd.none

  let update (msg: Msg) (state: State) =
    match msg with
    | SetNotes notes -> { state with Notes = notes }, Cmd.none
    | SetCurrentContent content ->
        { state with CurrentContent = content }, Cmd.none
    | ValidateContent ->
        printfn "Validating %s" state.CurrentContent
        let cmd = Cmd.none
        state, cmd
    | Error err ->
        eprintfn "%s" err.Message
        state, Cmd.none

  let view (state: State) (dispatch: Dispatch<Msg>) =
    let currentContentTxt = "Escribe algo..."
    let submitBtnTxt = "Guardar"
    article [] [
      form [
             attr.``class`` "row flex-spaces"
             on.submit (fun _ -> ValidateContent |> dispatch)
           ] [
        fieldset [ attr.``class`` "form-group" ] [
          label [ attr.``for`` "current-content" ] [
            text currentContentTxt
          ]
          textarea [
                     attr.id "current-content"
                     attr.placeholder currentContentTxt
                     bind.input.string
                       state.CurrentContent
                       (SetCurrentContent >> dispatch)
                   ] []
        ]
        button [
                 attr.``type`` "submit"
                 attr.disabled (state.CurrentContent.Length = 0)
               ] [
          text submitBtnTxt
        ]
      ]
    ]


  type Page() =
    inherit ProgramComponent<State, Msg>()

    override _.Program = Program.mkProgram init update view
