namespace Mandadin.Views

open Elmish
open Microsoft.JSInterop
open Bolero
open Bolero.Html
open Bolero.Remoting.Client
open Mandadin
open Microsoft.AspNetCore.Components


[<RequireQualifiedAccess>]
module Import =

  let parseContentString (content: string) : array<array<obj>> =
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

  type ShareDataPayload =
    { Text: string
      Title: string
      Url: string }

  type State = { ShareData: Option<ShareDataPayload> }


  type Msg =
    | RequestImportData
    | RequestImportDataSuccess of ShareDataPayload
    | ImportResult of Result<string * string, unit>
    | CreateFromImport of title: string * items: obj array array
    | CreateListSuccess of TrackList
    | Error of exn


  let private init (_: 'arg) =
    { ShareData = None }, Cmd.ofMsg RequestImportData


  let private update
    (msg: Msg)
    (state: State)
    (goToList: Option<string -> unit>)
    (js: IJSRuntime)
    =
    match msg with
    | RequestImportData ->
        state,
        Cmd.OfJS.either
          js
          "Mandadin.Share.ImportShareData"
          [||]
          RequestImportDataSuccess
          Error
    | RequestImportDataSuccess data ->
        let share = Some data
        { state with ShareData = share }, Cmd.none
    | ImportResult result ->
        match result with
        | Ok (title, content) ->
            let items = parseContentString content
            state, Cmd.ofMsg (CreateFromImport(title, items))
        | Result.Error () ->

            { state with ShareData = None }, Cmd.none
    | CreateFromImport (title, items) ->
        state,
        Cmd.OfJS.either
          js
          "Mandadin.Database.ImportList"
          [| title; items |]
          CreateListSuccess
          Error
    | CreateListSuccess trackList ->
        goToList
        |> Option.iter (fun goToList -> goToList trackList.Id)

        state, Cmd.none
    | Error err ->
        eprintfn "%O" err
        { state with ShareData = None }, Cmd.none

  let view (state: State) (dispatch: Dispatch<Msg>) =
    let data =
      state.ShareData
      |> Option.defaultValue ({ Title = ""; Text = ""; Url = "" })

    article [] [
      a [ attr.href "/"
          attr.``class`` "paper-btn btn-small" ] [
        Icon.Get Back None
      ]
      if state.ShareData.IsNone then
        p [] [
          text
            "No pudimos obtener informacion de lo que nos querias compartir 😢"
        ]
      Modals.ImportTrackList
        state.ShareData.IsSome
        (ImportResult >> dispatch)
        (Some data.Title)
        (Some data.Text)
    ]


  type Page() as this =
    inherit ProgramComponent<State, Msg>()

    [<Parameter>]
    member val OnGoToListRequested: Option<string -> unit> = None with get, set

    override _.Program =
      let update msg state =
        update msg state this.OnGoToListRequested this.JSRuntime


      Program.mkProgram init update view
#if DEBUG
      |> Program.withConsoleTrace
#endif
