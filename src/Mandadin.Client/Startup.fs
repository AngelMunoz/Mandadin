namespace Mandadin.Client

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Components.WebAssembly.Hosting


module Program =

  [<EntryPoint>]
  let Main args =
    task {
      let builder =
        WebAssemblyHostBuilder.CreateDefault(args)

      builder.RootComponents.Add<AppShell>("#main")

      let level =
        if builder.HostEnvironment.Environment = "Production" then
          LogLevel.Information
        else
          LogLevel.Debug

      builder.Logging.SetMinimumLevel(level) |> ignore

      builder.Logging.AddFilter(
        "Microsoft.AspNetCore.Components.RenderTree.*",
        LogLevel.Warning
      )
      |> ignore


      builder.Services
        .AddSingleton<ITrackListItemService>(Services.ListItems.factory)
        .AddSingleton<INoteService>(Services.Notes.factory)
        .AddSingleton<IShareService>(Services.Share.factory)
      |> ignore

      let app = builder.Build()

      do! app.RunAsync()
    }
    |> Async.AwaitTask
    |> Async.Start

    0
