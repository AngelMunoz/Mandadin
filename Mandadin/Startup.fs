open System
open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.JSInterop
open Mandadin

let builder =
  WebAssemblyHostBuilder.CreateDefault(Environment.GetCommandLineArgs())

builder.RootComponents.Add<Main.Mandadin>("#main")

builder.Services.AddScoped<IThemeService> (fun services ->
  let jsRuntime = services.GetService<IJSRuntime>()
  ThemeService.GetService jsRuntime)
|> ignore

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
  TrackListItemService.GetService jsRuntime share clipboard)
|> ignore

builder.Build().RunAsync()
|> Async.AwaitTask
|> Async.Start
