using Dapper.Contrib.Extensions;
using DokanNet;
using SpawnDev.DB;
using SpawnDev.WebFS.DokanAsync;
using System.Security.AccessControl;
using FileAccess = DokanNet.FileAccess;
using FileOptions = System.IO.FileOptions;

namespace SpawnDev.WebFS.Host
{
    /// <summary>
    /// This class redirects Dokan calls over websockets to remote IAsyncDokanOperations running on web sites that have been enabled by the user.
    /// </summary>
    public class WebFSServer : IAsyncDokanOperations
    {
        public int ConnectedDomainsCount => ConnectedDomains.Count;
        public int DomainsCount => DomainProviders.Count;
        public int ConnectedDomainsEnabled => EnabledConnections.Select(o => o.RequestOrigin.Host).Distinct().Count();
        public int ConnectedDomainsUndecided => UndecidedConnections.Select(o => o.RequestOrigin.Host).Distinct().Count();
        public int ConnectedDomainsDisabled => DisabledConnections.Select(o => o.RequestOrigin.Host).Distinct().Count();
        public string Status => $"{ConnectedDomainsUndecided}❓ {ConnectedDomainsEnabled}✔️ {ConnectedDomainsDisabled}❌";
        public List<string> ConnectedDomains => WebSocketServer.Connections.Select(o => o.RequestOrigin.Host).ToList();
        public List<WebSocketConnection> UndecidedConnections
        {
            get
            {
                var disallowedHosts = DomainProviders.Values.ToList().Where(o => o.Enabled == null).Select(o => o.Host).ToList();
                var conns = WebSocketServer.Connections.ToList();
                var disabledConnections = conns.Where(o => disallowedHosts.Contains(o.RequestOrigin.Host)).ToList();
                return disabledConnections;
            }
        }
        public List<WebSocketConnection> DisabledConnections
        {
            get
            {
                var disallowedHosts = DomainProviders.Values.ToList().Where(o => o.Enabled != true).Select(o => o.Host).ToList();
                var conns = WebSocketServer.Connections.ToList();
                var disabledConnections = conns.Where(o => disallowedHosts.Contains(o.RequestOrigin.Host)).ToList();
                return disabledConnections;
            }
        }
        public List<WebSocketConnection> EnabledConnections
        {
            get
            {
                var allowedHosts = DomainProviders.Values.ToList().Where(o => o.Enabled == true).Select(o => o.Host).ToList();
                var conns = WebSocketServer.Connections.ToList();
                var enabledConnections = conns.Where(o => allowedHosts.Contains(o.RequestOrigin.Host)).ToList();
                return enabledConnections;
            }
        }
        public Dictionary<string, DomainProvider> DomainProviders { get; } = new Dictionary<string, DomainProvider>();
        WebSocketServer WebSocketServer;
        public string VolumeLabel { get; } = "WebFS";
        IServiceProvider ServiceProvider;
        AppDB AppDB;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public WebFSServer(AppDB appDB, IServiceProvider serviceProvider, ushort port = 6565)
        {
            AppDB = appDB;
            ServiceProvider = serviceProvider;
            WebSocketServer = new WebSocketServer(ServiceProvider, port);
            WebSocketServer.OnConnectRequest += WebSocketServer_OnConnectRequest;
            WebSocketServer.OnConnected += WebSocketServer_OnConnected;
            WebSocketServer.OnDisconnected += WebSocketServer_OnDisconnected;
            using var conn = AppDB.GetConn();
            conn.CreateTableIfNotExists<DomainProvider>();
            DomainProviders = conn.GetAll<DomainProvider>().ToDictionary(o => o.Host, o => o);
            WebSocketServer.StartListening();
        }
        void UpdateDomainPerm(DomainProvider perm)
        {
            using var conn = AppDB.GetConn();
            conn.Update<DomainProvider>(perm);
        }
        DomainProvider SaveNewPerm(WebSocketConnection wsConn)
        {
            var host = wsConn.RequestOrigin.Host;
            using var conn = AppDB.GetConn();
            var perm = new DomainProvider
            {
                Host = host,
                FirstSeen = DateTimeOffset.Now,
                LastSeen = DateTimeOffset.Now,
                Url = wsConn.RequestOrigin.GetLeftPart(UriPartial.Authority)
            };
            conn.Insert<DomainProvider>(perm);
            return perm;
        }
        private void WebSocketServer_OnDisconnected(WebSocketServer sender, WebSocketConnection conn)
        {
            Console.WriteLine($"WebSocketServer_OnDisconnected: {conn.ConnectionId}");
        }
        public event Action<DomainProvider> NewDomainPerm = default!;
        private void WebSocketServer_OnConnected(WebSocketServer sender, WebSocketConnection conn)
        {
            Console.WriteLine($"WebSocketServer_OnConnected: {conn.ConnectionId} {conn.UserAgent}");
            if (!DomainProviders.TryGetValue(conn.RequestOrigin.Host, out var value))
            {
                value = SaveNewPerm(conn);
                DomainProviders[conn.RequestOrigin.Host] = value;
                NewDomainPerm?.Invoke(value);
            }
            else
            {
                value.LastSeen = DateTimeOffset.Now;
                value.Url = conn.RequestOrigin.GetLeftPart(UriPartial.Authority);
                UpdateDomainPerm(value);
            }
        }
        public DomainProvider? GetDomainAllowed(string host)
        {
            return DomainProviders.TryGetValue(host, out var value) ? value : null;
        }
        public void SetDomainAllowed(string host, bool enabled)
        {
            if (DomainProviders.TryGetValue(host, out var value))
            {
                value.Enabled = enabled;
                UpdateDomainPerm(value);
            }
        }
        private void WebSocketServer_OnConnectRequest(WebSocketServer sender, ConnectionRequestArgs eventArgs)
        {
            var url = eventArgs.Context.Request.Url;
            var userAgent = eventArgs.Context.Request.UserAgent;
            var origin = eventArgs.Context.Request.Headers.GetValues("origin")?.FirstOrDefault();
            Uri? originUri = null;
            try
            {
                originUri = string.IsNullOrEmpty(origin) ? null : new Uri(origin);
            }
            catch { }
            if (originUri == null)
            {
                eventArgs.CancelConnection = true;
            }
            Console.WriteLine($"WebSocketServer_OnConnectRequest: {eventArgs.ConnectionId} {origin} {url} {userAgent}");
        }
        #region IAsyncDokanOperations member
        bool SendCleanup = true;
        bool SendCloseFile = true;
        public async Task Cleanup(string filename, AsyncDokanFileInfo info)
        {
            if (GetProvider(filename, out var fHost, out var fPath, out var conn))
            {
                if (SendCleanup)
                {
                    try
                    {
                        await conn!.Run<IAsyncDokanOperations>(s => s.Cleanup(fPath, info));
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }
        public async Task CloseFile(string filename, AsyncDokanFileInfo info)
        {
            if (GetProvider(filename, out var fHost, out var fPath, out var conn))
            {
                if (SendCloseFile)
                {
                    try
                    {
                        await conn!.Run<IAsyncDokanOperations>(s => s.CloseFile(fPath, info));
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }
        public async Task<CreateFileResult> CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, AsyncDokanFileInfo info)
        {
            //if (info.IsDirectory && mode == FileMode.CreateNew) return DokanResult.AccessDenied;
            var isDesktopIni = filename.Contains("desktop.ini");
            if (filename.Contains("LOCALHOST"))
            {
                var nmt = true;
            }
            if (GetProvider(filename, out var fHost, out var fPath, out var conn))
            {
                try
                {
                    var tmp = await conn!.Run<IAsyncDokanOperations, CreateFileResult>(s => s.CreateFile(fPath, access, share, mode, options, attributes, info));
                    if (tmp != null)
                    {
                        return tmp;
                    }
                }
                catch (Exception ex)
                {
                    return DokanResult.Error;
                }
            }
            else if (filename == "\\" && mode == FileMode.Open)
            {
                return DokanResult.Success;
            }
            return DokanResult.FileNotFound;
        }
        public async Task<DokanAsyncResult> DeleteDirectory(string filename, AsyncDokanFileInfo info)
        {
            if (GetProvider(filename, out var fHost, out var fPath, out var conn))
            {
                try
                {
                    var tmp = await conn!.Run<IAsyncDokanOperations, DokanAsyncResult>(s => s.DeleteDirectory(fPath, info));
                    if (tmp != null)
                    {
                        return tmp;
                    }
                }
                catch (Exception ex)
                {
                    return DokanResult.Error;
                }
            }
            return DokanResult.Error;
        }
        public async Task<DokanAsyncResult> DeleteFile(string filename, AsyncDokanFileInfo info)
        {
            if (GetProvider(filename, out var fHost, out var fPath, out var conn))
            {
                try
                {
                    var tmp = await conn!.Run<IAsyncDokanOperations, DokanAsyncResult>(s => s.DeleteFile(fPath, info));
                    if (tmp != null)
                    {
                        return tmp;
                    }
                }
                catch (Exception ex)
                {
                    return DokanResult.Error;
                }
            }
            return DokanResult.Error;
        }
        public async Task<DokanAsyncResult> FlushFileBuffers(string filename, AsyncDokanFileInfo info)
        {
            return DokanResult.NotImplemented;
        }
        bool GetProvider(string filename, out string host, out string path, out WebSocketConnection? conn)
        {
            var parts = filename.Split('\\', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count == 0)
            {
                host = "";
                path = "";
                conn = null;
            }
            else
            {
                var fHost = parts[0];
                host = fHost;
                parts.RemoveAt(0);
                path = string.Join("/", parts);
                conn = GetEnabledHostPrimaryConnection(fHost);
            }
            return conn != null;
        }
        WebSocketConnection? GetEnabledHostPrimaryConnection(string host)
        {
            return EnabledConnections.OrderBy(o => o.WhenConnected).FirstOrDefault(o => o.IsConnected && o.RequestOrigin.Host.Equals(host, StringComparison.OrdinalIgnoreCase));
        }
        public async Task<FindFilesResult> FindFiles(string filename, AsyncDokanFileInfo info)
        {
            if (filename == "\\")
            {
                var files = new List<FileInformation>();
                var hosts = EnabledConnections.Select(o => o.RequestOrigin.Host).Distinct().ToList();
                foreach (var host in hosts)
                {
                    var conn = GetEnabledHostPrimaryConnection(host);
                    if (conn == null) continue;
                    var finfo = new FileInformation
                    {
                        FileName = host,
                        Attributes = FileAttributes.Directory,
                        LastAccessTime = DateTime.Now,
                        LastWriteTime = conn.WhenConnected,
                        CreationTime = conn.WhenConnected
                    };
                    files.Add(finfo);
                }
                return new FindFilesResult(DokanResult.Success, files);
            }
            else
            {
                if (GetProvider(filename, out var fHost, out var fPath, out var conn))
                {
                    try
                    {
                        var tmp = await conn!.Run<IAsyncDokanOperations, FindFilesResult>(s => s.FindFiles(fPath, info));
                        if (tmp != null)
                        {
                            return tmp;
                        }
                    }
                    catch (Exception ex)
                    {
                        return DokanResult.Error;
                    }
                }
                return DokanResult.Error;
            }
        }
        public async Task<FindFilesResult> FindFilesWithPattern(string filename, string searchPattern, AsyncDokanFileInfo info)
        {
            return DokanResult.NotImplemented;
        }
        DateTime Startup = DateTime.Now;
        public async Task<GetFileInformationResult> GetFileInformation(string filename, AsyncDokanFileInfo info)
        {
            if (filename == "\\")
            {
                var fileinfo = new FileInformation { FileName = filename };
                fileinfo.Attributes = FileAttributes.Directory;
                fileinfo.LastAccessTime = DateTime.Now;
                fileinfo.LastWriteTime = Startup;
                fileinfo.CreationTime = Startup;
                return new GetFileInformationResult(DokanResult.Success, fileinfo);
            }
            if (GetProvider(filename, out var fHost, out var fPath, out var conn))
            {
                if (string.IsNullOrEmpty(fPath))
                {

                    var fileinfo = new FileInformation { FileName = filename };
                    fileinfo.Attributes = FileAttributes.Directory;
                    fileinfo.LastAccessTime = DateTime.Now;
                    fileinfo.LastWriteTime = conn!.WhenConnected;
                    fileinfo.CreationTime = conn!.WhenConnected;
                    return new GetFileInformationResult(DokanResult.Success, fileinfo);
                }
                else
                {
                    try
                    {
                        var tmp = await conn!.Run<IAsyncDokanOperations, GetFileInformationResult>(s => s.GetFileInformation(fPath, info));
                        if (tmp != null)
                        {
                            var fileinfo = new FileInformation { FileName = filename };
                            fileinfo.Attributes = tmp.FileInfo.Attributes;
                            fileinfo.LastAccessTime = tmp.FileInfo.LastAccessTime;
                            fileinfo.LastWriteTime = tmp.FileInfo.LastWriteTime;
                            fileinfo.CreationTime = tmp.FileInfo.CreationTime;
                            fileinfo.Length = tmp.FileInfo.Length;
                            tmp.FileInfo = fileinfo;
                            return tmp;
                        }
                    }
                    catch (Exception ex)
                    {
                        return DokanResult.Error;
                    }
                }
            }
            return DokanResult.Error;
        }
        public async Task<DokanAsyncResult> LockFile(string filename, long offset, long length, AsyncDokanFileInfo info)
        {
            if (GetProvider(filename, out var fHost, out var fPath, out var conn))
            {
                if (!string.IsNullOrEmpty(fPath))
                {
                    try
                    {
                        var tmp = await conn!.Run<IAsyncDokanOperations, DokanAsyncResult>(s => s.LockFile(fPath, offset, length, info));
                        if (tmp != null)
                        {
                            return tmp;
                        }
                    }
                    catch (Exception ex)
                    {
                        return DokanResult.Error;
                    }
                }
            }
            return DokanResult.Error;
        }
        public async Task<DokanAsyncResult> UnlockFile(string filename, long offset, long length, AsyncDokanFileInfo info)
        {
            if (GetProvider(filename, out var fHost, out var fPath, out var conn))
            {
                if (!string.IsNullOrEmpty(fPath))
                {
                    try
                    {
                        var tmp = await conn!.Run<IAsyncDokanOperations, DokanAsyncResult>(s => s.UnlockFile(fPath, offset, length, info));
                        if (tmp != null)
                        {
                            return tmp;
                        }
                    }
                    catch (Exception ex)
                    {
                        return DokanResult.Error;
                    }
                }
            }
            return DokanResult.Error;
        }
        public async Task<ReadFileResult> ReadFile(string filename, long offset, long maxCount, AsyncDokanFileInfo info)
        {
            if (GetProvider(filename, out var fHost, out var fPath, out var conn))
            {
                if (!string.IsNullOrEmpty(fPath))
                {
                    try
                    {
                        var tmp = await conn!.Run<IAsyncDokanOperations, ReadFileResult>(s => s.ReadFile(fPath, offset, maxCount, info));
                        if (tmp != null)
                        {
                            return tmp;
                        }
                    }
                    catch (Exception ex)
                    {
                        return DokanResult.Error;
                    }
                }
            }
            return DokanResult.Error;
        }
        public async Task<WriteFileResult> WriteFile(string filename, byte[] buffer, long offset, AsyncDokanFileInfo info)
        {
            if (GetProvider(filename, out var fHost, out var fPath, out var conn))
            {
                if (!string.IsNullOrEmpty(fPath))
                {
                    try
                    {
                        var tmp = await conn!.Run<IAsyncDokanOperations, WriteFileResult>(s => s.WriteFile(fPath, buffer, offset, info));
                        if (tmp != null)
                        {
                            return tmp;
                        }
                    }
                    catch (Exception ex)
                    {
                        return DokanResult.Error;
                    }
                }
            }
            return DokanResult.Error;
        }
        public async Task<DokanAsyncResult> SetEndOfFile(string filename, long length, AsyncDokanFileInfo info)
        {
            if (GetProvider(filename, out var fHost, out var fPath, out var conn))
            {
                if (!string.IsNullOrEmpty(fPath))
                {
                    try
                    {
                        var tmp = await conn!.Run<IAsyncDokanOperations, DokanAsyncResult>(s => s.SetEndOfFile(fPath, length, info));
                        if (tmp != null)
                        {
                            return tmp;
                        }
                    }
                    catch (Exception ex)
                    {
                        return DokanResult.Error;
                    }
                }
            }
            return DokanResult.Error;
        }
        public async Task<DokanAsyncResult> SetAllocationSize(string filename, long length, AsyncDokanFileInfo info)
        {
            if (GetProvider(filename, out var fHost, out var fPath, out var conn))
            {
                if (!string.IsNullOrEmpty(fPath))
                {
                    try
                    {
                        var tmp = await conn!.Run<IAsyncDokanOperations, DokanAsyncResult>(s => s.SetAllocationSize(fPath, length, info));
                        if (tmp != null)
                        {
                            return tmp;
                        }
                    }
                    catch (Exception ex)
                    {
                        return DokanResult.Error;
                    }
                }
            }
            return DokanResult.Error;
        }
        public async Task<DokanAsyncResult> SetFileAttributes(string filename, FileAttributes attr, AsyncDokanFileInfo info)
        {
            if (GetProvider(filename, out var fHost, out var fPath, out var conn))
            {
                if (!string.IsNullOrEmpty(fPath))
                {
                    try
                    {
                        var tmp = await conn!.Run<IAsyncDokanOperations, DokanAsyncResult>(s => s.SetFileAttributes(fPath, attr, info));
                        if (tmp != null)
                        {
                            return tmp;
                        }
                    }
                    catch (Exception ex)
                    {
                        return DokanResult.Error;
                    }
                }
            }
            return DokanResult.Error;
        }
        public async Task<DokanAsyncResult> SetFileTime(string filename, DateTime? ctime, DateTime? atime, DateTime? mtime, AsyncDokanFileInfo info)
        {
            if (GetProvider(filename, out var fHost, out var fPath, out var conn))
            {
                if (!string.IsNullOrEmpty(fPath))
                {
                    try
                    {
                        var tmp = await conn!.Run<IAsyncDokanOperations, DokanAsyncResult>(s => s.SetFileTime(fPath, ctime, atime, mtime, info));
                        if (tmp != null)
                        {
                            return tmp;
                        }
                    }
                    catch (Exception ex)
                    {
                        return DokanResult.Error;
                    }
                }
            }
            return DokanResult.Error;
        }
        #region Unsupported
        public async Task<DokanAsyncResult> MoveFile(string filename, string newname, bool replace, AsyncDokanFileInfo info)
        {
            // TODO
            // check the hsots of both paths
            // if they are the same, send the move event to that host and let it handle it,
            // otherwise, manually copy the file ...
            var succ1 = GetProvider(filename, out var fHost, out var fPath, out var conn);
            var succ2 = GetProvider(newname, out var fHostNew, out var fPathNew, out var connNew);
            if (succ1 && succ2 && conn == connNew)
            {
                if (!string.IsNullOrEmpty(fPath))
                {
                    try
                    {
                        var tmp = await conn!.Run<IAsyncDokanOperations, DokanAsyncResult>(s => s.MoveFile(fPath, fPathNew, replace, info));
                        if (tmp != null)
                        {
                            return tmp;
                        }
                    }
                    catch (Exception ex)
                    {
                        return DokanResult.Error;
                    }
                }
            }
            else
            {
                // TODO
                var nmt = true;
            }
            return DokanResult.Error;
        }
        public async Task<GetFileSecurityResult> GetFileSecurity(string filename, AccessControlSections sections, AsyncDokanFileInfo info)
        {
            return DokanResult.NotImplemented;
        }
        public async Task<DokanAsyncResult> SetFileSecurity(string filename, FileSystemSecurity security, AccessControlSections sections, AsyncDokanFileInfo info)
        {
            return DokanResult.NotImplemented;
        }
        public async Task<FindStreamsResult> FindStreams(string filename, AsyncDokanFileInfo info)
        {
            return DokanResult.NotImplemented;
        }
        #endregion
        #region Drive Ops
        public async Task<GetVolumeInformationResult> GetVolumeInformation(AsyncDokanFileInfo info)
        {
            return new GetVolumeInformationResult(DokanResult.Success, VolumeLabel, FileSystemFeatures.None, "", 256);
        }
        public async Task<DokanAsyncResult> Mounted(string mountPoint, AsyncDokanFileInfo info)
        {
            return DokanResult.Success;
        }
        public async Task<DokanAsyncResult> Unmounted(AsyncDokanFileInfo info)
        {
            return DokanResult.Success;
        }
        public async Task<GetDiskFreeSpaceResult> GetDiskFreeSpace(AsyncDokanFileInfo info)
        {
            var freeBytesAvailable = 512 * 1024 * 1024;
            var totalBytes = 1024 * 1024 * 1024;
            var totalFreeBytes = 512 * 1024 * 1024;
            return new GetDiskFreeSpaceResult(DokanResult.Success, freeBytesAvailable, totalBytes, totalFreeBytes);
        }
        #endregion
        #endregion IAsyncDokanOperations member
    }
}
