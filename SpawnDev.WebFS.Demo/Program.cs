using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.WebWorkers;
using SpawnDev.WebFS;
using SpawnDev.WebFS.Demo;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add SpawnDev.BlazorJS JS  interop
builder.Services.AddBlazorJSRuntime(out var JS);
// WebWorkerService allows running the file system provider in a worker if we want
builder.Services.AddWebWorkerService();

// Registers WebFSProvider, the demo WebFS provider as WebFSProvider and IAsyncDokanOperations
// Registers WebFSClient which connects to the tray app when its Enabled property is set to true
builder.Services.AddWebFS<WebFSProvider>();

// if set to true, the WebFSProvider service will be enabled in a web worker instead of the window
var runProviderInWorker = false;

if (JS.IsWindow)
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}
// Startup using BlazorJSRunAsync
var host = await builder.Build().StartBackgroundServices();
if (JS.IsWindow)
{
    if (runProviderInWorker)
    {
        // running in a window with runProviderInWorker == true. start a worker to host the fs
        // NOTE: Currently the ui will not display activity if the provider runs in a worker
        var WebWorkerService = host.Services.GetRequiredService<WebWorkerService>();
        var webWorker = await WebWorkerService.GetWebWorker();
        await webWorker!.Set<WebFSProvider, bool>(WebFSProvider => WebFSProvider.Enabled, true);
    }
    else
    {
        // running in a window with runProviderInWorker == false. start the fs provider here in the window
        var WebFSProvider = host.Services.GetRequiredService<WebFSProvider>();
        WebFSProvider.Enabled = true;
    }
}
await host.BlazorJSRunAsync();