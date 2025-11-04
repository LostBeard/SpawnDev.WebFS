# SpawnDev.WebFS
[![NuGet](https://img.shields.io/nuget/dt/SpawnDev.WebFS.svg?label=SpawnDev.WebFS)](https://www.nuget.org/packages/SpawnDev.WebFS) 

SpawnDev.WebFS lets Blazor WebAssembly web apps host a file system via a domain labeled folder by connecting to 
the SpawnDev.WebFS.Host app running on the user's PC.

## SpawnDev.WebFS.Tray
<img width="48" src="https://github.com/LostBeard/SpawnDev.WebFS/raw/refs/heads/master/SpawnDev.WebFS/wwwroot/webfs-128.png" />  

The SpawnDev.WebFS.Tray app runs on the user's PC with an icon in the system tray and can optionally start with Windows. 
While running, the WebFS host app uses [DokanNet](https://github.com/dokan-dev/dokan-dotnet) to mount a new drive on the user's PC 
that can be accessed normally by any apps on the user's computer. Web sites can request permission to
provide a file system via a domain labeled folder on the root of the new drive.

**WebFS Tray Download:**  [SpawnDevWebFSSetup](https://github.com/LostBeard/SpawnDev.WebFS/raw/refs/heads/master/Setup/SpawnDevWebFSSetup.exe)

## WebFS Providers
Website file system providers list with links and descriptions.
### [WebFS Demo Provider](https://lostbeard.github.io/SpawnDev.WebFS/)
> Demonstrates WebFS by providing read and write access to the browsers [Origin private file system](https://developer.mozilla.org/en-US/docs/Web/API/File_System_API/Origin_private_file_system).


## Use cases
- A website that allows protected access to a remote encrypted file system.
- Team collaboration on shared projects.
- Browser extension that provides access to Google Photos through a folder by automating Google Photos in a browser page.
- Access files and folders on your remote devices anywhere using normal apps. 
- Etc.

## WIP
If you are interested in this project, please start an issue to suggest features or areas of interest.
