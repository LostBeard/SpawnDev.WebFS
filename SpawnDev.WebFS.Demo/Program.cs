using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpawnDev.BlazorJS;
using SpawnDev.WebFS;
using SpawnDev.WebFS.Demo;
using SpawnDev.WebFS.DokanAsync;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
// Add SpawnDev.BlazorJS interop
builder.Services.AddBlazorJSRuntime(out var JS);

// WebFSClient connects to the WebFS tray app running on the user's PC
// It will use the registered IAsyncDokanOperations service to handle requests from WebFS tray
builder.Services.AddSingleton<WebFSClient>();

// Register our custom WebFS filesystem provider WebFSProvider, which implements IAsyncDokanOperations
// WebFSProvider is a demo  WebFS provider that allows read and write access to the browser's Origin private file system
builder.Services.AddSingleton<WebFSProvider>();

// Register WebFSProvider as IAsyncDokanOperations also sp WebFSClient can use it
builder.Services.AddSingleton<IAsyncDokanOperations>(sp => sp.GetRequiredService<WebFSProvider>());

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
