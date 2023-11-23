namespace Mandadin.Client.Views

open System
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop

open Elmish

open Bolero
open Bolero.Html
open Bolero.Remoting.Client

open Mandadin.Client

[<RequireQualifiedAccess>]
module Import =

  let inline parseContentString (content: string) =
    content.Split(Environment.NewLine)
    |> Parse.entries

  [<Struct>]
  type ShareDataPayload =
    { Text: string
      Title: string
      Url: string }

  [<Struct>]
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
    (logger: ILogger)
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
        match parseContentString content with
        | Ok items -> state, Cmd.ofMsg (CreateFromImport(title, items))
        | Result.Error errs ->
          errs
          |> List.iter (fun (line, err) ->
            logger.LogDebug(
              "Failed at {index} with: {error} for '{line}'",
              err.idx,
              err.message,
              line
            ))

          state, Cmd.none
      | Result.Error() -> { state with ShareData = ValueNone }, Cmd.none
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
      logger.LogDebug("Error: {error}", err)
      { state with ShareData = ValueNone }, Cmd.none

  let view (state: State) (dispatch: Dispatch<Msg>) =

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


  type Page() =
    inherit ProgramComponent<State, Msg>()

    [<Inject>]
    member val LoggerFactory = Unchecked.defaultof<ILoggerFactory> with get, set

    [<Parameter>]
    member val OnGoToListRequested: string -> unit = ignore with get, set

    override self.Program =
      let update msg state =
        let logger =
          self.LoggerFactory.CreateLogger("Import Page")

        update msg state self.OnGoToListRequested self.JSRuntime logger


      Program.mkProgram init update view
#if DEBUG
      |> Program.withConsoleTrace
#endif
