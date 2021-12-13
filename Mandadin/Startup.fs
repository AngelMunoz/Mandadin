namespace Mandadin

open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.JSInterop


module Program =

  [<EntryPoint>]
  let Main args =
    let builder =
      WebAssemblyHostBuilder.CreateDefault(args)

    builder.RootComponents.Add<Main.Mandadin>("#main")

    builder.Services.AddScoped<IClipboardService> (fun services ->
      let jsRuntime = services.GetService<IJSRuntime>()
      Clipboard.GetService jsRuntime)
    |> ignore

    builder.Services.AddScoped<IShareService> (fun services ->
      let jsRuntime = services.GetService<IJSRuntime>()
      Share.GetService jsRuntime)
    |> ignore

    builder.Services.AddScoped<ITrackListItemService> (fun services ->
      let jsRuntime = services.GetService<IJSRuntime>()
      let clipboard = services.GetService<IClipboardService>()
      let share = services.GetService<IShareService>()
      TrackListItem.GetService jsRuntime share clipboard)
    |> ignore

    builder.Build().RunAsync()
    |> Async.AwaitTask
    |> Async.Start
    |> ignore

    0
