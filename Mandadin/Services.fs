namespace Mandadin

open Mandadin
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop

[<RequireQualifiedAccess>]
module Items =
  let stringifyItems (items: list<TrackListItem>) : string =
    let isDoneToX (isDone: bool) = if isDone then 'x' else ' '

    let itemToLine item =
      sprintf "[ %c ] %s" (isDoneToX item.IsDone) item.Name

    let stringified =
      items
      |> Array.ofList
      |> Array.Parallel.map itemToLine

    System.String.Join('\n', stringified)


module JsModuleIdentifiers =

  [<RequireQualifiedAccess>]
  module ThemeIdentifiers =

    [<Literal>]
    let SetDocumentTitle = "Mandadin.Theme.SetDocumentTitle"

    [<Literal>]
    let SwitchTheme = "Mandadin.Theme.SwitchTheme"

    [<Literal>]
    let HasOverlayControls = "Mandadin.Theme.HasOverlayControls"

    [<Literal>]
    let GetTheme = "Mandadin.Theme.GetTheme"

  [<RequireQualifiedAccess>]
  module Share =
    [<Literal>]
    let CanShare = "Mandadin.Share.CanShare"

    [<Literal>]
    let ShareContent = "Mandadin.Share.ShareContent"

    [<Literal>]
    let ImportShareData = "Mandadin.Share.ImportShareData"

  [<RequireQualifiedAccess>]
  module Clipboard =
    [<Literal>]
    let CopyTextToClipboard = "Mandadin.Clipboard.CopyTextToClipboard"

    [<Literal>]
    let ReadTextFromClipboard =
      "Mandadin.Clipboard.ReadTextFromClipboard"

  [<RequireQualifiedAccess>]
  module Database =
    [<Literal>]
    let GetListItems = "Mandadin.Database.GetListItems"

    [<Literal>]
    let CreateListItem = "Mandadin.Database.CreateListItem"

    [<Literal>]
    let UpdateListItem = "Mandadin.Database.UpdateListItem"

    [<Literal>]
    let DeleteListItem = "Mandadin.Database.DeleteListItem"

    [<Literal>]
    let ListItemExists = "Mandadin.Database.ListItemExists"


    [<Literal>]
    let FindNote = "Mandadin.Database.FindNote"

    [<Literal>]
    let FindNotes = "Mandadin.Database.FindNotes"

    [<Literal>]
    let CreateNote = "Mandadin.Database.CreateNote"

    [<Literal>]
    let UpdateNote = "Mandadin.Database.UpdateNote"

    [<Literal>]
    let DeleteNote = "Mandadin.Database.DeleteNote"

    [<Literal>]
    let FindLists = "Mandadin.Database.FindLists"

    [<Literal>]
    let FindList = "Mandadin.Database.FindList"

    [<Literal>]
    let ListNameExists = "Mandadin.Database.ListNameExists"

    [<Literal>]
    let CreateList = "Mandadin.Database.CreateList"

    [<Literal>]
    let ImportList = "Mandadin.Database.ImportList"

    [<Literal>]
    let DeleteList = "Mandadin.Database.DeleteList"

    [<Literal>]
    let SaveHideDone = "Mandadin.Database.SaveHideDone"

    [<Literal>]
    let GetHideDone = "Mandadin.Database.GetHideDone"

[<RequireQualifiedAccess>]
module ThemeService =
  open JsModuleIdentifiers

  let GetService (jsRuntime: IJSRuntime) =
    { new IThemeService with
        member _.SetDocumentTitle title =
          jsRuntime
            .InvokeAsync(ThemeIdentifiers.SetDocumentTitle, title)
            .AsTask()

        member _.SwitchTheme theme =
          task {
            let theme = theme.AsString

            return! jsRuntime.InvokeAsync(ThemeIdentifiers.SwitchTheme, theme)
          }

        member _.GetTheme() =
          task {
            let! theme = jsRuntime.InvokeAsync(ThemeIdentifiers.GetTheme)

            return
              match theme with
              | "Light" -> Theme.Light
              | "Dark" -> Theme.Dark
              | _ -> Theme.Dark
          }

        member _.HasOverlayControls() =
          jsRuntime
            .InvokeAsync(ThemeIdentifiers.HasOverlayControls)
            .AsTask() }

[<RequireQualifiedAccess>]
module Clipboard =
  open JsModuleIdentifiers

  let GetService (jsRuntime: IJSRuntime) =
    { new IClipboardService with
        member _.SendToClipboard content =
          task {
            let! result =
              jsRuntime.InvokeAsync(Clipboard.CopyTextToClipboard, content)

            return result
          }

        member _.GetFromClipboard() =
          task {
            let! result = jsRuntime.InvokeAsync(Clipboard.ReadTextFromClipboard)

            return result
          } }

[<RequireQualifiedAccess>]
module Share =
  open JsModuleIdentifiers

  let GetService (jsRuntime: IJSRuntime) =
    { new IShareService with
        member _.GetCanShare() =
          task {
            let! result = jsRuntime.InvokeAsync(Share.CanShare)

            return result
          }

        member _.ShareContent(title, content, ?url) =
          task {
            printfn "%s %s" title content

            let! result =
              match url with
              | None ->
                jsRuntime.InvokeAsync(Share.ShareContent, title, content)
              | Some url ->
                jsRuntime.InvokeAsync(Share.ShareContent, title, content, url)

            return result
          }

        member _.ImportShareContent() =
          task {
            let! result = jsRuntime.InvokeAsync(Share.ImportShareData)

            return result
          } }

[<RequireQualifiedAccess>]
module TrackListItemService =

  open JsModuleIdentifiers

  let GetService
    (jsRuntime: IJSRuntime)
    (share: IShareService)
    (clipboard: IClipboardService)
    =
    { new ITrackListItemService with
        member _.Find(listId, hideDone) =
          task {
            let! result =
              jsRuntime.InvokeAsync(
                Database.GetListItems,
                listId :> obj,
                hideDone :> obj
              )

            return result |> List.ofSeq
          }

        member _.Create(listId, name) =
          task {
            let! result =
              jsRuntime.InvokeAsync(Database.CreateListItem, listId, name)

            return result
          }

        member _.Update item =
          task {
            let! result = jsRuntime.InvokeAsync(Database.UpdateListItem, item)

            return result
          }

        member _.Delete item =
          task {
            let! result = jsRuntime.InvokeAsync(Database.DeleteListItem, item)

            return result
          }

        member _.Exists(listId, listName) =
          task {
            let! result =
              jsRuntime.InvokeAsync(Database.ListItemExists, listId, listName)

            return result
          }

        member _.ShareItems(listId, items) =
          let stringified = Items.stringifyItems items
          share.ShareContent(listId, stringified, None)

        member _.SendToClipboard items =
          let stringified = Items.stringifyItems items
          clipboard.SendToClipboard stringified }

[<RequireQualifiedAccess>]
module TrackListService =
  open JsModuleIdentifiers

  let GetService (jsRuntime: IJSRuntime) =
    { new ITrackListService with
        override _.CreateList name =
          jsRuntime
            .InvokeAsync(Database.CreateList, name)
            .AsTask()

        override _.DeleteList(name, revision) =
          jsRuntime
            .InvokeAsync(Database.DeleteList, name, revision)
            .AsTask()

        override _.FindList name =
          jsRuntime
            .InvokeAsync(Database.FindList, name)
            .AsTask()

        override _.FindLists() =
          jsRuntime.InvokeAsync(Database.FindLists).AsTask()

        override _.ImportList(name: string, items: obj array array) =
          jsRuntime
            .InvokeAsync(Database.ImportList, name, items)
            .AsTask()

        override _.ListNameExists name =
          jsRuntime
            .InvokeAsync(Database.ListNameExists, name)
            .AsTask()

        override _.SaveHideDone(listId, hideDone) =
          jsRuntime
            .InvokeAsync(Database.SaveHideDone, listId, hideDone)
            .AsTask()

        override _.GetHideDone listId =
          jsRuntime
            .InvokeAsync(Database.GetHideDone, listId)
            .AsTask() }


[<RequireQualifiedAccess>]
module NoteService =
  open JsModuleIdentifiers

  let GetService (jsRuntime: IJSRuntime) =
    { new INoteService with
        override _.CreateNote content =
          jsRuntime
            .InvokeAsync(Database.CreateNote, content)
            .AsTask()

        override _.DeleteNote(noteId, revision) =
          jsRuntime
            .InvokeAsync(Database.DeleteNote, noteId, revision)
            .AsTask()

        override _.FindNote noteId =
          jsRuntime
            .InvokeAsync(Database.FindNote, noteId)
            .AsTask()

        override _.FindNotes() =
          jsRuntime.InvokeAsync(Database.FindNotes).AsTask()

        override _.UpdateNote note =
          jsRuntime
            .InvokeAsync(Database.UpdateNote, note)
            .AsTask() }

[<RequireQualifiedAccess>]
module Navigation =
  let GetService (nav: NavigationManager) =
    { new IRoutingService with
        override _.NavigateTo route =
          match route with
          | Notes -> nav.NavigateTo("/notes")
          | Lists -> nav.NavigateTo("/lists")
          | ListItem listId -> nav.NavigateTo($"/lists/{listId}") }
