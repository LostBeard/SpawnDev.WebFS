using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpawnDev;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.WebWorkers;
using SpawnDev.WebFS;
using SpawnDev.WebFS.Demo;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add SpawnDev.BlazorJS JS interop
builder.Services.AddBlazorJSRuntime(out var JS);
// WebWorkerService allows running the file system provider in a worker if we want
builder.Services.AddWebWorkerService();

// Registers WebFSProvider, the demo WebFS provider as WebFSProvider and IAsyncDokanOperations
// Registers WebFSClient which connects to the tray app when its Enabled property is set to true
builder.Services.AddWebFS<WebFSProvider>();

// If this is a window, add window componenets
if (JS.IsWindow)
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}

// Start background services that need it
var host = await builder.Build().StartBackgroundServices();

// If this is a window, start the file system provider based on scope we want to run it in
if (JS.IsWindow)
{
    // providerScope options:
    // Window (default if not one of others) - runs in the window scope. Best for testing.
    // DedicatedWorker - runs in a dedicated worker. Breakpoints and debugging not supported in workers.
    // SharedWorker - runs in a shared worker. Breakpoints and debugging not supported in workers.
    var providerScope = GlobalScope.Window;
    if (providerScope == GlobalScope.DedicatedWorker)
    {
        // run in a DedicatedWorker
        // NOTE: Currently the ui will not display activity if the provider runs in a worker
        var WebWorkerService = host.Services.GetRequiredService<WebWorkerService>();
        var webWorker = await WebWorkerService.GetWebWorker();
        // start the provider in the dedicatd worker
        await webWorker!.Set<WebFSProvider, bool>(WebFSProvider => WebFSProvider.Enabled, true);
    }
    else if (providerScope == GlobalScope.SharedWorker)
    {
        // run in a SharedWorker
        // NOTE: Currently the ui will not display activity if the provider runs in a worker
        var WebWorkerService = host.Services.GetRequiredService<WebWorkerService>();
        var webWorker = await WebWorkerService.GetSharedWebWorker("FileSystemProvider");
        // start the provider in the shared worker
        await webWorker!.Set<WebFSProvider, bool>(WebFSProvider => WebFSProvider.Enabled, true);
    }
    else
    { 
        // run in the Window scope
        var WebFSProvider = host.Services.GetRequiredService<WebFSProvider>();
        WebFSProvider.Enabled = true;
    }
}
// Startup using BlazorJSRunAsync
await host.BlazorJSRunAsync();