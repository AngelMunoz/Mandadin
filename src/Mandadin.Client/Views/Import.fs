namespace Mandadin.Client.Views

open Elmish
open Microsoft.JSInterop
open Bolero
open Bolero.Html
open Bolero.Remoting.Client
open Mandadin.Client
open Microsoft.AspNetCore.Components


[<RequireQualifiedAccess>]
module Import =
  open System

  let parseContentString (content: string) : array<array<obj>> =
    let parseRow (row: string) =
      let split = row.Split("] ")

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

  type State =
    { ShareData: ValueOption<ShareDataPayload> }


  type Msg =
    | RequestImportData
    | RequestImportDataSuccess of ShareDataPayload
    | ImportResult of Result<string * string, unit>
    | CreateFromImport of title: string * items: obj array array
    | CreateListSuccess of TrackList
    | Error of exn


  let private init (_: 'arg) =
    { ShareData = ValueNone }, Cmd.ofMsg RequestImportData


  let private update
    (msg: Msg)
    (state: State)
    (goToList: string -> unit)
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
      let share = ValueSome data
      { state with ShareData = share }, Cmd.none
    | ImportResult result ->
      match result with
      | Ok(title, content) ->
        let items = parseContentString content
        state, Cmd.ofMsg (CreateFromImport(title, items))
      | Result.Error() ->

        { state with ShareData = ValueNone }, Cmd.none
    | CreateFromImport(title, items) ->
      state,
      Cmd.OfJS.either
        js
        "Mandadin.Database.ImportList"
        [| title; items |]
        CreateListSuccess
        Error
    | CreateListSuccess trackList ->
      goToList trackList.Id
      state, Cmd.none
    | Error err ->
      eprintfn "%O" err
      { state with ShareData = ValueNone }, Cmd.none

  let view (state: State) (dispatch: Dispatch<Msg>) =
    let data =
      state.ShareData
      |> ValueOption.defaultValue ({ Title = ""; Text = ""; Url = "" })

    article {
      a {
        attr.href "/"
        Icon.Get Back
      }

      cond state.ShareData
      <| function
        | ValueNone ->
          p {
            text
              "No pudimos obtener informacion de lo que nos querias compartir 😢"
          }
        | _ -> empty ()

      cond state.ShareData
      <| function
        | ValueSome data ->
          let prefill: Modals.Import.ImportData =
            { title = data.Title
              content = data.Text }

          comp<Modals.Import.ImportTrackList> {
            "ImportData" => prefill

            "OnDismiss"
            => fun _ -> dispatch (ImportResult(Result.Error()))

            "OnImport"
            => fun (data: Modals.Import.ImportData) ->
              dispatch (ImportResult(Ok(data.title, data.content)))
          }
        | ValueNone -> empty ()
    }


  type Page() as this =
    inherit ProgramComponent<State, Msg>()

    [<Parameter>]
    member val OnGoToListRequested: string -> unit = ignore with get, set

    override _.Program =
      let update msg state =
        update msg state this.OnGoToListRequested this.JSRuntime


      Program.mkProgram init update view
#if DEBUG
      |> Program.withConsoleTrace
#endif
