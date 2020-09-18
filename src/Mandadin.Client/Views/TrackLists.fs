namespace Mandadin.Client.Views

open Elmish
open Bolero
open Mandadin.Client
open Microsoft.AspNetCore.Components


[<RequireQualifiedAccess>]
module ListItems =
  type State =
    {
      Items: list<TrackListItem>
      TrackListId: Option<string>
    }

  type Msg = SetItems of list<TrackListItem>

  let init (listId: Option<string>) =
    { Items = []; TrackListId = None }, Cmd.none

  let update (msg: Msg) (state: State) =
    match msg with
    | SetItems list -> { state with Items = list }, Cmd.none

  let view (state: State) (dispatch: Dispatch<Msg>) = Html.article [] []


  type Page() =
    inherit ProgramComponent<State, Msg>()

    [<Parameter>]
    member val ListId: Option<string> = None with get, set

    override this.Program =
      let init _ = init this.ListId
      Program.mkProgram init update view


[<RequireQualifiedAccess>]
module Lists =

  type State = { TrackLists: list<TrackList> }

  type Msg = SetTrackList of list<TrackList>

  let init (_: 'arg) = { TrackLists = list.Empty }, Cmd.none

  let update (msg: Msg) (state: State) =
    match msg with
    | SetTrackList tracklists ->
        { state with TrackLists = tracklists }, Cmd.none

  let view (state: State) (dispatch: Dispatch<Msg>) = Html.article [] []


  type Page() =
    inherit ProgramComponent<State, Msg>()

    override _.Program = Program.mkProgram init update view
