namespace Mandadin.Client.Services

open System

open Microsoft.JSInterop
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

open IcedTasks
open FsToolkit.ErrorHandling

open Mandadin.Client

module Share =
  let inline factory (services: IServiceProvider) : IShareService =
    let jsRuntime =
      services.GetService<IJSRuntime>()

    let loggerFactory =
      services.GetService<ILoggerFactory>()

    let logger =
      loggerFactory.CreateLogger<IShareService>()

    { new IShareService with
        member _.FromClipboard() =
          valueTask {
            logger.LogDebug("Getting content from clipboard...")

            try
              let! content =
                jsRuntime.InvokeAsync<string>(
                  "Mandadin.Clipboard.ReadTextFromClipboard",
                  [||]
                )

              return content
            with exn ->
              logger.LogError(
                "Failed to get content from clipboard: {exn}",
                exn
              )

              return ""
          }

        member _.ShareTracklistItem
          (
            listId: string,
            content: string
          ) : Threading.Tasks.ValueTask =
          valueTaskUnit {
            logger.LogDebug("Sharing list: {listId}", listId)
            logger.LogDebug("Content: '{content}'...", content.Substring(0, 20))

            do!
              jsRuntime.InvokeVoidAsync(
                "Mandadin.Share.ShareContent",
                [| listId :> obj; content |]
              )
          }

        member _.ShareNote(content: string) : Threading.Tasks.ValueTask =
          valueTaskUnit {
            logger.LogDebug(
              "Sharing Content: '{content}'...",
              content.Substring(0, 20)
            )

            do!
              jsRuntime.InvokeVoidAsync(
                "Mandadin.Share.ShareContent",
                [| "Nota..." :> obj; content |]
              )
          }

        member _.ToClipboard(content: string) : Threading.Tasks.ValueTask =
          valueTaskUnit {
            logger.LogDebug(
              "Copying content: '{content}'...",
              content.Substring(0, 20)
            )

            do!
              jsRuntime.InvokeVoidAsync(
                "Mandadin.Clipboard.CopyTextToClipboard",
                [| content :> obj |]
              )
          } }

module Notes =
  open System.Threading.Tasks

  let inline factory (services: IServiceProvider) : INoteService =
    let jsRuntime =
      services.GetService<IJSRuntime>()

    let loggerFactory =
      services.GetService<ILoggerFactory>()

    let logger =
      loggerFactory.CreateLogger<INoteService>()


    { new INoteService with
        member _.CreateNote(content: string) =
          valueTask {
            logger.LogDebug("Creating note...")

            try
              let! note =
                jsRuntime.InvokeAsync<Note>(
                  "Mandadin.Database.CreateNote",
                  [| content :> obj |]
                )

              return ValueSome note
            with exn ->
              logger.LogError("Failed to create note: {exn}", exn)

              return ValueNone
          }

        member _.DeleteNote(note: Note) =
          valueTaskUnit {
            logger.LogDebug("Deleting note: {note}", note)

            return!
              jsRuntime.InvokeVoidAsync(
                "Mandadin.Database.DeleteNote",
                [| note.Id :> obj; note.Rev |]
              )
          }

        member _.UpdateNote(content, note) =
          valueTask {
            logger.LogDebug("Updating note: {note}", note)

            try
              let! updated =
                jsRuntime.InvokeAsync<Note>(
                  "Mandadin.Database.UpdateNote",
                  [| { note with Content = content } :> obj |]
                )

              return ValueSome updated
            with exn ->
              logger.LogError("Failed to update note: {exn}", exn)

              return ValueNone
          }

        member _.GetNotes() =
          valueTask {
            logger.LogDebug("Getting notes...")

            try
              let! notes =
                jsRuntime.InvokeAsync<list<Note>>(
                  "Mandadin.Database.FindNotes",
                  [||]
                )

              return notes
            with exn ->
              logger.LogError("Failed to get notes: {exn}", exn)

              return List.empty
          } }

module ListItems =


  let inline factory (services: IServiceProvider) : ITrackListItemService =
    let jsRuntime =
      services.GetService<IJSRuntime>()

    let loggerFactory =
      services.GetService<ILoggerFactory>()

    let logger =
      loggerFactory.CreateLogger<ITrackListItemService>()

    { new ITrackListItemService with
        member _.GetHideDone listId =
          valueTask {

            let! hideDone =
              jsRuntime.InvokeAsync<bool>(
                "Mandadin.Database.GetHideDone",
                [| listId :> obj |]
              )

            return hideDone

          }

        member _.SetHideDone(listId, hideDone) =
          valueTaskUnit {
            do!
              jsRuntime.InvokeVoidAsync(
                "Mandadin.Database.SaveHideDone",
                [| listId :> obj; hideDone |]
              )
          }

        member self.CreateItem(listId, name) =
          taskResult {
            logger.LogDebug("Creating item: {listId}, {name}", listId, name)

            do!
              String.IsNullOrWhiteSpace name
              |> Result.requireFalse EmtptyString

            do!
              self.ItemExists(listId, name).AsTask()
              |> TaskResult.requireFalse (ExistingItem name)

            try

              let! created =
                jsRuntime.InvokeAsync<TrackListItem>(
                  "Mandadin.Database.CreateListItem",
                  [| listId :> obj; name |]
                )

              return created
            with exn ->
              logger.LogError(
                "Failed to create item: {listId}, {name}, error: {exn}",
                listId,
                name,
                exn
              )

              return! exn |> CreationFailed |> Error
          }


        member _.DeleteItem item =
          valueTaskUnit {
            do!
              jsRuntime.InvokeVoidAsync(
                "Mandadin.Database.DeleteListItem",
                [| item.Id :> obj |]
              )
          }


        member _.GetItems(listId, ?hideDone) =
          valueTask {
            let hideDone = defaultArg hideDone false

            try
              let! items =
                jsRuntime.InvokeAsync<list<TrackListItem>>(
                  "Mandadin.Database.GetListItems",
                  [| listId :> obj; hideDone |]
                )

              return items
            with ex ->
              logger.LogError(
                "Failed to get items for list: {listId}, error: {ex}",
                listId,
                ex
              )

              return List.empty
          }

        member _.ItemExists(listId, name) =
          valueTask {
            try
              let! exists =
                jsRuntime.InvokeAsync<bool>(
                  "Mandadin.Database.ListItemExists",
                  [| listId :> obj; name |]
                )

              return exists
            with ex ->
              logger.LogError(
                "Failed to check if item exists: {listId}, {name}, error: {ex}",
                listId,
                name,
                ex
              )

              return false
          }

        member _.UpdateItem item =
          valueTask {
            try
              let! updated =
                jsRuntime.InvokeAsync<TrackListItem>(
                  "Mandadin.Database.UpdateListItem",
                  [| item :> obj |]
                )

              return updated
            with ex ->
              logger.LogError(
                "Failed to update item: {item}, error: {ex}",
                item,
                ex
              )

              return item
          } }
