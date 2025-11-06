# SpawnDev.WebFS
[![NuGet](https://img.shields.io/nuget/dt/SpawnDev.WebFS.svg?label=SpawnDev.WebFS)](https://www.nuget.org/packages/SpawnDev.WebFS) 

SpawnDev.WebFS lets Blazor WebAssembly web apps host a file system via a domain labeled folder by connecting to 
the SpawnDev.WebFS.Host app running on the user's PC.

## SpawnDev.WebFS.Tray
<img width="48" src="https://github.com/LostBeard/SpawnDev.WebFS/raw/refs/heads/master/SpawnDev.WebFS/wwwroot/webfs-128.png" />  

The SpawnDev.WebFS.Tray app runs on the user's PC with an icon in the system tray and can optionally start with Windows. 
While running, the WebFS host app uses [DokanNet](https://github.com/dokan-dev/dokan-dotnet) to mount a new drive on the user's PC 
that can be accessed normally by any apps on the user's computer. Websites can request permission to
provide a file system via a domain labeled folder on the root of the new drive.

**WebFS Tray Download:**  [SpawnDevWebFSSetup](https://github.com/LostBeard/SpawnDev.WebFS/raw/refs/heads/master/Setup/SpawnDevWebFSSetup.exe)

## WebFS Providers
Website file system provider links and descriptions.

### [lostbeard.github.io - Demo WebFS Provider](https://lostbeard.github.io/SpawnDev.WebFS/)
> Demonstrates WebFS by providing read and write access to the browser's [Origin private file system](https://developer.mozilla.org/en-US/docs/Web/API/File_System_API/Origin_private_file_system).

## Use cases
- A website that allows protected access to a remote encrypted file system.
- Team collaboration on shared projects.
- Browser extension that provides access to Google Photos through a folder by automating Google Photos in a browser page.
- Access files and folders on your remote devices anywhere using normal apps. 
- Etc.

## Using SpawnDev.WebFS in a Blazor WebAssembly app

#### WebFS tray app
The WebFS tray app is required for both development and production use. In development, it may be more useful to use the source project in this repo instead.

#### Blazor WebAssembly
- Add a reference to the latest `SpawnDev.WebFS` Nuget package using your method of choice.  

`Demo.csproj`
```xml
    <PackageReference Include="SpawnDev.WebFS" Version="1.0.0" />
```

- Register required services and your file system provider class that implements IAsyncDokanOperations

`Program.cs` - From demo repo
```cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpawnDev.BlazorJS;
using SpawnDev.WebFS;
using SpawnDev.WebFS.Demo;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add SpawnDev.BlazorJS JS interop
builder.Services.AddBlazorJSRuntime(out var JS);

// Registers WebFSProvider, the demo WebFS provider as WebFSProvider and IAsyncDokanOperations
// Registers WebFSClient which connects to the tray app when its Enabled property is set to true
builder.Services.AddWebFS<WebFSProvider>();

// Start using BlazorJSRunAsync
await builder.Build().BlazorJSRunAsync();
```

- Implement the `IAsyncDokanOperations` interface in your custom provider and set WebFSClient.Enabled = true when ready  

`WebFSProvider.cs` - From demo repo
```cs
    /// <summary>
    /// Demo WebFS filesystem provider.<br/>
    /// Provides access the the browsers Origin private file system.<br/>
    /// IBackgroundService sets this service to autostart when the Blazor WASM web app loads
    /// </summary>
    [RemoteCallable]
    public class WebFSProvider : IAsyncDokanOperations, IBackgroundService
    {
        public WebFSProvider(BlazorJSRuntime js, WebFSClient webFSClient)
        {
            JS = js;
            WebFSClient = webFSClient;
            // this demo provider uses navigator.storage to provide access to the browser's origin private file system
            using var navigator = JS.Get<Navigator>("navigator");
            StorageManager = navigator.Storage;
            // tell WebFSClient to connect to the tray app when it can
            WebFSClient.Enabled = true;
        }

        // Implement IAsyncDokanOperations interface
        public async Task<CreateFileResult> CreateFile(string filename, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, AsyncDokanFileInfo info)
        {
            ...
```

## WIP
If you are interested in this project, please start an issue to suggest features or areas of interest.
