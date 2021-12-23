open System
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.JSInterop
open Mandadin

let builder =
  WebAssemblyHostBuilder.CreateDefault(Environment.GetCommandLineArgs())

builder
  .AddFunBlazorNode("#main", Main.View())
  .Services.AddFunBlazor()
|> ignore

let inline getService<'Service> (services: IServiceProvider) =
  services.GetService<'Service>()

// get the service and combine the result into the GetService from the modules
// fun services -> services |> getService<'Type> |> Module.GetService
builder
  .Services
  .AddScoped<IThemeService>(getService<IJSRuntime> >> ThemeService.GetService)
  .AddScoped<IClipboardService>(getService<IJSRuntime> >> Clipboard.GetService)
  .AddScoped<IShareService>(getService<IJSRuntime> >> Share.GetService)
  .AddScoped<ITrackListService>(
    getService<IJSRuntime>
    >> TrackListService.GetService
  )
  .AddScoped<INoteService>(getService<IJSRuntime> >> NoteService.GetService)
  .AddScoped<IRoutingService>(getService<NavigationManager> >> Navigation.GetService)
  .AddScoped<ITrackListItemService>(fun services ->
    (getService<IJSRuntime> services,
     getService<IShareService> services,
     getService<IClipboardService> services)
    |||> TrackListItemService.GetService)
|> ignore

builder.Build().RunAsync()
|> Async.AwaitTask
|> Async.Start
