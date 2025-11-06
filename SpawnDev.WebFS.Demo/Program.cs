using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpawnDev.BlazorJS;
using SpawnDev.WebFS;
using SpawnDev.WebFS.Demo;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add SpawnDev.BlazorJS JS  interop
builder.Services.AddBlazorJSRuntime(out var JS);

// Registers WebFSProvider, the demo WebFS provider as WebFSProvider and IAsyncDokanOperations
// Registers WebFSClient which connects to the tray app when its Enabled property is set to true
builder.Services.AddWebFS<WebFSProvider>();

// Startup using BlazorJSRunAsync
await builder.Build().BlazorJSRunAsync();