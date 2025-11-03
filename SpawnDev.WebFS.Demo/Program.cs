using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpawnDev.BlazorJS;
using SpawnDev.WebFS;
using SpawnDev.WebFS.Demo;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
// Add SpawnDev.BlazorJS interop
builder.Services.AddBlazorJSRuntime(out var JS);
builder.Services.AddSingleton<WebFSClient>();
builder.Services.AddSingleton<WebFSProvider>();

// Add dom objects if running in a window
if (JS.IsWindow)
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}

// Start
var host = await builder.Build().StartBackgroundServices();
#if DEBUG

#endif
// Run app using BlazorJSRunAsync extension method
await host.BlazorJSRunAsync();
