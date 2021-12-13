namespace Mandadin

open System.Threading.Tasks
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

[<RequireQualifiedAccess>]
module Clipboard =
  open JsModuleIdentifiers

  let GetService (jsRuntime: IJSRuntime) =
    { new IClipboardService with
        member _.SendToClipboard content =
          task {
            let! result =
              jsRuntime.InvokeAsync(
                Clipboard.CopyTextToClipboard,
                [| content |]
              )

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
            let args =
              match url with
              | Some url -> [| title; content; url |]
              | None -> [| title; content |]

            let! result = jsRuntime.InvokeAsync(Share.ShareContent, args)

            return result
          }

        member this.ShareContent(title, content) =
          this.ShareContent(title, content)

        member _.ImportShareContent() =
          task {
            let! result = jsRuntime.InvokeAsync(Share.ImportShareData)

            return result
          } }

[<RequireQualifiedAccess>]
module TrackListItem =

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
                [| listId :> obj; hideDone :> obj |]
              )

            return result |> List.ofSeq
          }

        member _.Create(listId, name) =
          task {
            let! result =
              jsRuntime.InvokeAsync(Database.CreateListItem, [| listId; name |])

            return result
          }

        member _.Update item =
          task {
            let! result =
              jsRuntime.InvokeAsync(Database.UpdateListItem, [| item |])

            return result
          }

        member _.Delete item =
          task {
            let! result =
              jsRuntime.InvokeAsync(Database.DeleteListItem, [| item |])

            return result
          }

        member _.Exists(listId, listName) =
          task {
            let! result =
              jsRuntime.InvokeAsync(
                Database.ListItemExists,
                [| listId; listName |]
              )

            return result
          }

        member _.ShareItems(listId, items) =
          let stringified = Items.stringifyItems items
          share.ShareContent(listId, stringified)

        member _.SendToClipboard items =
          let stringified = Items.stringifyItems items
          clipboard.SendToClipboard stringified }
