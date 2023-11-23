namespace Mandadin.Client

open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Components.WebAssembly.Hosting

module Program =

  [<EntryPoint>]
  let Main args =
    task {
      let builder =
        WebAssemblyHostBuilder.CreateDefault(args)

      builder.RootComponents.Add<Main.Mandadin>("#main")

      let level =
        if builder.HostEnvironment.Environment = "Production" then
          LogLevel.Information
        else
          LogLevel.Debug

      builder.Logging.AddFilter(
        "Microsoft.AspNetCore.Components.RenderTree.*",
        LogLevel.Warning
      )
      |> ignore

      builder.Logging.SetMinimumLevel(level) |> ignore

      let app = builder.Build()

      do! app.RunAsync()
    }
    |> ignore

    0
