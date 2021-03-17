namespace Mandadin.Client

open Microsoft.AspNetCore.Components.WebAssembly.Hosting

module Program =

    [<EntryPoint>]
    let Main args =
        let builder = WebAssemblyHostBuilder.CreateDefault(args)
        builder.RootComponents.Add<Main.Mandadin>("#main")
        builder.Build().RunAsync() |> Async.AwaitTask |> Async.Start |> ignore
        0
