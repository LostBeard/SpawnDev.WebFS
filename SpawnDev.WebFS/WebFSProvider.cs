using DokanNet;
using Microsoft.AspNetCore.Components;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;
using SpawnDev.BlazorJS.WebWorkers;
using SpawnDev.WebFS.DokanAsync;
using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using FileAccess = DokanNet.FileAccess;
using FileOptions = System.IO.FileOptions;

namespace SpawnDev.WebFS
{
    /// <summary>
    /// Demo WebFS filesystem provider.<br/>
    /// Provides access the the browsers Origin private file system.<br/>
    /// </summary>
    [RemoteCallable]
    public class WebFSProvider : IAsyncDokanOperations, IBackgroundService
    {
        BlazorJSRuntime JS;
        /// <summary>
        /// The host name serving this app
        /// </summary>
        public string Host { get; }
        /// <summary>
        /// Called when a file context is opened
        /// </summary>
        public event Action<OpenFileContext> OnFileOpened = default!;
        /// <summary>
        /// Called when a file context is closed
        /// </summary>
        public event Action<OpenFileContext> OnFileClosed = default!;
        /// <summary>
        /// Storage manager provides access to the file system source this WWebFS provider uses
        /// </summary>
        protected StorageManager StorageManager { get; }
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="js"></param>
        /// <param name="navigationManager"></param>
        public WebFSProvider(BlazorJSRuntime js, NavigationManager navigationManager)
        {
            JS = js;
            Host = new Uri(navigationManager.BaseUri).Host;
            using var navigator = JS.Get<Navigator>("navigator");
            StorageManager = navigator.Storage;
        }
        protected Dictionary<string, OpenFileContext> _openFiles = new Dictionary<string, OpenFileContext>();
        /// <summary>
        /// Currently open files
        /// </summary>
        public List<OpenFileContext> OpenFiles => _openFiles.Values.ToList();
        protected OpenFileContext CreateContext(string filename, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, AsyncDokanFileInfo info)
        {
            var openFile = new OpenFileContext(filename, access, share, mode, options, attributes, info);
            openFile.Info = info;
            _openFiles[info.OpId] = openFile;
            OnFileOpened?.Invoke(openFile);
            return openFile;
        }
        protected bool GetContext(AsyncDokanFileInfo info, out OpenFileContext? openFile)
        {
            if (_openFiles.TryGetValue(info.OpId, out openFile))
            {
                if (openFile.Info.DeleteOnClose != info.DeleteOnClose)
                {
                    var nmt = true;
                }
                if (openFile.Info.WriteToEndOfFile != info.WriteToEndOfFile)
                {
                    var nmt = true;
                }
                return true;
            }
            return false;
        }
        protected async Task CloseContext(string opid)
        {
            if (_openFiles.TryGetValue(opid, out var openFile))
            {
                _openFiles.Remove(opid);
                //JS.Log($"CloseContext: {opid} {openFile.Filename}", openFile.Info);
                if (openFile.Context is FileSystemWritableFileStream str)
                {
                    await str.Close();
                    str.Dispose();
                }
                else if (openFile.Context is IDisposable fileStream)
                {
                    // returning AccessDenied, cleanup and close won't be called,
                    // so we have to take care of the stream now
                    fileStream.Dispose();
                    openFile.Context = null;
                }
                // check if we need to delete the file system object
                if (openFile.Info.DeleteOnClose)
                {
                    if (openFile.Info.IsDirectory)
                    {
                        try
                        {
                            using var FS = await GetPathDirectoryHandle();
                            await FS!.RemovePath(openFile.Filename, true);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    else
                    {
                        try
                        {
                            using var FS = await GetPathDirectoryHandle();
                            await FS!.RemovePath(openFile.Filename, true);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
                OnFileClosed?.Invoke(openFile);
            }
        }
        protected T Trace<T>(string method, OpenFileContext? openFile, T result) where T : DokanAsyncResult
        {
            if (result is GetFileInformationResult info)
            {
                JS.Log($"<< {method} {openFile?.Filename} {result.Status.ToString()}", info.FileInfo);
            }
            else if (method != nameof(CreateFile))
            {
                JS.Log($"<< {method} {openFile?.Filename} {result.Status.ToString()}");
            }
            if (openFile != null)
            {
                switch (method)
                {
                    case nameof(WebFSProvider.CreateFile):
                        if (result.Status != NtStatus.Success)
                        {
                            // Cleanup will not be called for this op id
                            _ = CloseContext(openFile.Info.OpId);
                        }
                        break;
                    case nameof(WebFSProvider.GetFileInformation):
                        break;
                    case nameof(FindFiles):

                        break;
                }
            }
            return result;
        }

        public async Task<FileSystemHandle?> GetPathHandle(string? path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                return await StorageManager.GetDirectory();
            }
            using var fsRoot = await StorageManager.GetDirectory();
            return await fsRoot.GetPathHandle(path);
        }
        public async Task<FileSystemFileHandle?> GetPathFileHandle(string path, bool create = false)
        {
            if (string.IsNullOrEmpty(path)) return null;
            using var fsRoot = await StorageManager.GetDirectory();
            return await fsRoot.GetPathFileHandle(path, create);
        }
        public async Task<FileSystemDirectoryHandle?> GetPathDirectoryHandle(string? path = null, bool create = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                return await StorageManager.GetDirectory();
            }
            using var fsRoot = await StorageManager.GetDirectory();
            return await fsRoot.GetPathDirectoryHandle(path, create);
        }

        /// <inheritdoc/>
        public async Task Cleanup(string filename, AsyncDokanFileInfo info)
        {
            JS.Log($"Cleanup: {info.OpId} {filename}", info);
            if (GetContext(info, out var ct))
            {
                if (info.DeleteOnClose && !ct!.Info.DeleteOnClose)
                {
                    ct.Info.DeleteOnClose = true;
                }
            }
            await CloseContext(info.OpId);
        }
        /// <inheritdoc/>
        public async Task CloseFile(string filename, AsyncDokanFileInfo info)
        {
            JS.Log($"CloseFile: {info.OpId} {filename}", info);
            await CloseContext(info.OpId);
        }
        /// <inheritdoc/>
        public async Task<CreateFileResult> CreateFile(string filename, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, AsyncDokanFileInfo info)
        {
            var ct = CreateContext(filename, access, share, mode, options, attributes, info);
            try
            {
                using var FS = await GetPathDirectoryHandle();
                using var entry = await GetPathHandle(filename);
                var pathExists = entry != null;
                var isFile = entry?.Kind == "file";
                var pathIsDirectory = entry?.Kind == "directory";
                JS.Log($"CreateFile: {info.OpId} {entry?.Kind} {pathIsDirectory} {filename} FileAccess:{access.ToString()} FileShare:{share.ToString()} FileMode:{mode.ToString()} FileOptions:{options.ToString()} FileAttributes:{attributes.ToString()}", ct);
                if (info.IsDirectory)
                {
                    switch (mode)
                    {
                        case FileMode.CreateNew:
                            if (pathExists) return Trace<CreateFileResult>(nameof(CreateFile), ct, DokanResult.FileExists);
                            // create directory
                            await FS.CreatePathDirectory(filename);
                            break;
                        case FileMode.Open:
                            if (isFile) return Trace<CreateFileResult>(nameof(CreateFile), ct, DokanResult.NotADirectory);
                            if (!pathExists) return Trace<CreateFileResult>(nameof(CreateFile), ct, DokanResult.FileNotFound);
                            // open (do nothing here)
                            break;
                        default:
                            return Trace<CreateFileResult>(nameof(CreateFile), ct, DokanResult.Error);
                    }
                }
                else
                {
                    var readWriteAttributes = (access & DataAccess) == 0;
                    var readAccess = (access & DataWriteAccess) == 0;
                    switch (mode)
                    {
                        case FileMode.CreateNew:
                            if (pathExists) return Trace<CreateFileResult>(nameof(CreateFile), ct, DokanResult.FileExists);
                            // create
                            break;
                        case FileMode.Open:
                            if (!pathExists) return Trace<CreateFileResult>(nameof(CreateFile), ct, DokanResult.FileNotFound);
                            // open
                            // check if driver only wants to read attributes, security info, or open directory
                            if (readWriteAttributes || pathIsDirectory)
                            {
                                if (pathIsDirectory && (access & FileAccess.Delete) == FileAccess.Delete && (access & FileAccess.Synchronize) != FileAccess.Synchronize)
                                {
                                    //It is a DeleteFile request on a directory
                                    return Trace<CreateFileResult>(nameof(CreateFile), ct, DokanResult.AccessDenied);
                                }
                                info.IsDirectory = pathIsDirectory;
                                return Trace<CreateFileResult>(nameof(CreateFile), ct, new CreateFileResult(DokanResult.Success, pathIsDirectory));
                            }
                            break;
                        case FileMode.Truncate:
                            if (!pathExists) return Trace<CreateFileResult>(nameof(CreateFile), ct, DokanResult.FileNotFound);
                            // open and truncate
                            await FS.Write(filename, "");
                            break;
                        case FileMode.Create:
                            // open and truncate, or create
                            await FS.Write(filename, "");
                            break;
                        case FileMode.Append:
                            // open and seek to end or create
                            if (!pathExists)
                            {
                                await FS.Write(filename, "");
                            }
                            break;
                        case FileMode.OpenOrCreate:
                            if (pathExists) return Trace<CreateFileResult>(nameof(CreateFile), ct, DokanResult.FileExists);
                            // open or create
                            if (!pathExists)
                            {
                                await FS.Write(filename, "");
                            }
                            break;
                    }
                    try
                    {
                        var streamAccess = readAccess ? System.IO.FileAccess.Read : System.IO.FileAccess.ReadWrite;
                        if (mode == FileMode.CreateNew && readAccess)
                        {
                            streamAccess = System.IO.FileAccess.ReadWrite;
                        }
                        //ct.Context = new FileStream("", mode, streamAccess, share, 4096, options);
                    }
                    catch (UnauthorizedAccessException) // don't have access rights
                    {
                        if (ct.Context is IDisposable fileStream)
                        {
                            // returning AccessDenied, cleanup and close won't be called,
                            // so we have to take care of the stream now
                            fileStream.Dispose();
                            ct.Context = null;
                        }
                        return Trace<CreateFileResult>(nameof(CreateFile), ct, DokanResult.AccessDenied);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        return Trace<CreateFileResult>(nameof(CreateFile), ct, DokanResult.PathNotFound);
                    }
                    catch (Exception ex)
                    {
                        var hr = (uint)Marshal.GetHRForException(ex);
                        switch (hr)
                        {
                            case 0x80070020: //Sharing violation
                                return Trace<CreateFileResult>(nameof(CreateFile), ct, DokanResult.SharingViolation);
                            default:
                                throw;
                        }
                    }
                }
                return Trace<CreateFileResult>(nameof(CreateFile), ct, new CreateFileResult(DokanResult.Success, pathIsDirectory));
            }
            catch (Exception ex)
            {
                return Trace<CreateFileResult>(nameof(CreateFile), ct, DokanResult.Error);
            }
            finally
            {

            }
        }
        protected const FileAccess DataAccess = FileAccess.ReadData | FileAccess.WriteData | FileAccess.AppendData |
                                              FileAccess.Execute |
                                              FileAccess.GenericExecute | FileAccess.GenericWrite |
                                              FileAccess.GenericRead;

        protected const FileAccess DataWriteAccess = FileAccess.WriteData | FileAccess.AppendData |
                                                   FileAccess.Delete |
                                                   FileAccess.GenericWrite;

        /// <inheritdoc/>
        public async Task<DokanAsyncResult> DeleteDirectory(string filename, AsyncDokanFileInfo info)
        {
            JS.Log($"DeleteDirectory: {info.OpId} {filename}", info);
            if (GetContext(info, out var ct))
            {
                try
                {
                    using var FS = await GetPathDirectoryHandle();
                    using var entry = await FS!.GetPathHandle(filename);
                    var pathExists = entry != null;
                    var isFile = entry?.Kind == "file";
                    var pathIsDirectory = entry?.Kind == "directory";
                    if (isFile)
                    {
                        return DokanResult.AccessDenied;
                    }
                    else if (!pathExists)
                    {
                        return DokanResult.FileNotFound;
                    }
                    else
                    {
                        if (ct!.Info.DeleteOnClose != info.DeleteOnClose)
                        {
                            ct!.Info.DeleteOnClose = info.DeleteOnClose;
                        }
                        return DokanResult.Success;
                    }
                }
                catch
                {

                }
            }
            return DokanResult.Error;
        }
        /// <summary>
        /// Check if it is possible to delete a file.<br/>
        /// You should NOT delete the file in DeleteFile, but instead you must only check whether you can delete the file or not, and return NtStatus.Success(when you can delete it) or appropriate error codes such as NtStatus.AccessDenied, NtStatus.ObjectNameNotFound.<br/>
        /// DeleteFile will also be called with IDokanFileInfo.DeletePending set to false to notify the driver when the file is no longer requested to be deleted.<br/>
        /// When you return NtStatus.Success, you get a Cleanup call afterwards with IDokanFileInfo.DeletePending set to true and only then you have to actually delete the file being closed.<br/>
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public async Task<DokanAsyncResult> DeleteFile(string filename, AsyncDokanFileInfo info)
        {
            JS.Log($"DeleteFile: {info.OpId} {filename}", info);
            if (GetContext(info, out var ct))
            {
                try
                {
                    using var FS = await GetPathDirectoryHandle();
                    using var entry = await FS.GetPathHandle(filename);
                    var pathExists = entry != null;
                    var isFile = entry?.Kind == "file";
                    var pathIsDirectory = entry?.Kind == "directory";
                    if (pathIsDirectory)
                    {
                        return DokanResult.AccessDenied;
                    }
                    else if (!pathExists)
                    {
                        return DokanResult.FileNotFound;
                    }
                    else
                    {
                        if (ct!.Info.DeleteOnClose != info.DeleteOnClose)
                        {
                            ct!.Info.DeleteOnClose = info.DeleteOnClose;
                        }
                        return DokanResult.Success;
                    }
                }
                catch
                {

                }
            }
            return DokanResult.Error;
        }
        /// <inheritdoc/>
        public async Task<FindFilesResult> FindFiles(string filename, AsyncDokanFileInfo info)
        {
            JS.Log($"FindFiles: {info.OpId} {filename}", info);
            try
            {
                var files = new List<FileInformation>();
                if (GetContext(info, out var ct))
                {

                    var entry = await GetPathHandle(filename);
                    var pathExists = entry != null;
                    var isFile = entry?.Kind == "file";
                    var pathIsDirectory = entry?.Kind == "directory";
                    if (entry is FileSystemDirectoryHandle dsDir)
                    {
                        var dirHandles = await dsDir.GetPathDirectoryHandles();
                        foreach (var handle in dirHandles)
                        {
                            var finfo = new FileInformation
                            {
                                FileName = handle.Name,
                                Attributes = FileAttributes.Directory,
                                LastAccessTime = DateTime.Now,
                                LastWriteTime = null,
                                CreationTime = null
                            };
                            files.Add(finfo);
                        }
                        var fileHandles = await dsDir.GetPathFileHandles();
                        foreach (var handle in fileHandles)
                        {
                            var finfo = new FileInformation
                            {
                                FileName = handle.Name,
                                Attributes = FileAttributes.Normal,
                                LastAccessTime = DateTime.Now,
                                LastWriteTime = null,
                                CreationTime = null,
                                Length = await handle.GetSize(),
                            };
                            files.Add(finfo);
                        }
                    }
                }
                return Trace(nameof(FindFiles), ct, new FindFilesResult(DokanResult.Success, files));
            }
            catch (Exception ex)
            {
                var nmt = true;
            }
            return Trace<FindFilesResult>(nameof(FindFiles), null, DokanResult.Error);
        }
        /// <inheritdoc/>
        public async Task<FindFilesResult> FindFilesWithPattern(string filename, string searchPattern, AsyncDokanFileInfo info)
        {
            JS.Log($"FindFilesWithPattern: {info.OpId} {filename}", info);

            return DokanResult.NotImplemented;
        }
        /// <inheritdoc/>
        public async Task<DokanAsyncResult> FlushFileBuffers(string filename, AsyncDokanFileInfo info)
        {
            JS.Log($"FlushFileBuffers: {info.OpId} {filename}", info);

            return DokanResult.NotImplemented;
        }
        /// <inheritdoc/>
        public async Task<GetFileInformationResult> GetFileInformation(string filename, AsyncDokanFileInfo info)
        {
            JS.Log($"GetFileInformation: {info.OpId} {filename}", info);
            GetContext(info, out var ct);
            using var entry = await GetPathHandle(filename);
            var pathExists = entry != null;
            var isFile = entry?.Kind == "file";
            var pathIsDirectory = entry?.Kind == "directory";
            if (!pathExists)
            {
                return Trace<GetFileInformationResult>(nameof(GetFileInformation), ct, DokanResult.FileNotFound);
            }
            long length = 0;
            long lastModified = 0;
            if (entry is FileSystemFileHandle fsHandle)
            {
                length = await fsHandle.GetSize();
                lastModified = await fsHandle.GetLastModified();
            }
            var fileinfo = new FileInformation { FileName = filename };
            fileinfo.Attributes = pathIsDirectory ? FileAttributes.Directory : FileAttributes.Normal;
            fileinfo.LastAccessTime = DateTime.Now;
            fileinfo.LastWriteTime = lastModified == 0 ? null : DateTimeOffset.FromUnixTimeMilliseconds(lastModified).UtcDateTime;
            fileinfo.CreationTime = lastModified == 0 ? null : DateTimeOffset.FromUnixTimeMilliseconds(lastModified).UtcDateTime;
            fileinfo.Length = length;
            return Trace<GetFileInformationResult>(nameof(GetFileInformation), ct, new GetFileInformationResult(DokanResult.Success, fileinfo));
        }
        /// <inheritdoc/>
        public async Task<DokanAsyncResult> LockFile(string filename, long offset, long length, AsyncDokanFileInfo info)
        {
            JS.Log($"LockFile: {info.OpId} {filename}", info);

            return DokanResult.NotImplemented;
        }
        /// <inheritdoc/>
        public async Task<DokanAsyncResult> UnlockFile(string filename, long offset, long length, AsyncDokanFileInfo info)
        {
            JS.Log($"UnlockFile: {info.OpId} {filename}", info);
            return DokanResult.NotImplemented;
        }
        /// <inheritdoc/>
        public async Task<ReadFileResult> ReadFile(string filename, long offset, long maxCount, AsyncDokanFileInfo info)
        {
            JS.Log($"ReadFile: {info.OpId} {filename}", info);

            if (GetContext(info, out var ct))
            {
                try
                {
                    using var fsHandle = await GetPathFileHandle(filename);
                    if (fsHandle == null)
                    {
                        return DokanResult.Error;
                    }
                    var size = await fsHandle.GetSize();
                    var bytesRead = Math.Max(0, Math.Min(size - offset, maxCount));
                    var data = new byte[bytesRead];
                    if (bytesRead > 0)
                    {
                        using var fileData = await fsHandle.ReadStream();
                        if (offset > 0) fileData.Position = offset;
                        await fileData.ReadExactlyAsync(data);
                    }
                    JS.Log($"Read {data.Length} bytes (maxCount: {maxCount}, offset: {offset}) from {filename}");
                    return new ReadFileResult(NtStatus.Success, data);
                }
                catch (Exception ex)
                {
                    JS.Log($"ReadFile error: {ex.ToString()}");
                }
            }
            return DokanResult.Error;
        }
        SemaphoreSlim writeLock = new SemaphoreSlim(1);
        /// <inheritdoc/>
        public async Task<WriteFileResult> WriteFile(string filename, byte[] buffer, long offset, AsyncDokanFileInfo info)
        {
            JS.Log($"WriteFile: {info.OpId} {filename}", offset, buffer.Length, info);
            if (GetContext(info, out var ct))
            {
                var releaseLock = false;
                try
                {
                    await writeLock.WaitAsync();
                    releaseLock = true;
                    var str = ct!.Context == null ? null : ct!.Context as FileSystemHandleWritableStream;
                    if (str == null)
                    {
                        using var st = await GetPathFileHandle(filename);
                        str = await FileSystemHandleWritableStream.Create(st!, true);
                        ct!.Context = str;
                    }
                    if (info.WriteToEndOfFile || ct.Info.WriteToEndOfFile)
                    {
                        var nmt = true;
                    }
                    str.Seek(offset, SeekOrigin.Begin);
                    await str.WriteAsync(buffer);
                    JS.Log($"Wrote {buffer.Length} bytes (offset: {offset}) to {filename}");
                    return new WriteFileResult(buffer.Length);
                }
                catch (Exception ex)
                {
                    JS.Log($"WriteFile error: {ex.ToString()}");
                }
                finally
                {
                    if (releaseLock) writeLock.Release();
                }
            }
            return DokanResult.Error;
        }
        /// <inheritdoc/>
        public async Task<DokanAsyncResult> SetAllocationSize(string filename, long length, AsyncDokanFileInfo info)
        {
            JS.Log($"SetAllocationSize: {info.OpId} {filename}", info);
            if (GetContext(info, out var ct))
            {
                try
                {
                    using var st = await GetPathFileHandle(filename, true);
                    using var str = await st!.CreateWritable(new FileSystemCreateWritableOptions { KeepExistingData = true });
                    if (info.WriteToEndOfFile)
                    {
                        var nmt = true;
                    }
                    if (length > 0)
                    {
                        await str.Truncate((ulong)length);
                    }
                    await str.Close();
                    JS.Log($"Allocated {length} bytes to {filename}");
                    return DokanResult.Success;
                }
                catch (Exception ex)
                {

                }
            }
            return DokanResult.Error;
        }
        /// <inheritdoc/>
        public async Task<DokanAsyncResult> SetEndOfFile(string filename, long length, AsyncDokanFileInfo info)
        {
            JS.Log($"SetEndOfFile: {info.OpId} {filename} {length}", info);
            if (GetContext(info, out var ct))
            {
                try
                {
                    using var st = await GetPathFileHandle(filename, true);
                    using var str = await st!.CreateWritable(new FileSystemCreateWritableOptions { KeepExistingData = true });
                    if (info.WriteToEndOfFile)
                    {
                        var nmt = true;
                    }
                    if (length > 0)
                    {
                        await str.Truncate((ulong)length);
                    }
                    await str.Close();
                    return DokanResult.Success;
                }
                catch (Exception ex)
                {

                }
            }
            return DokanResult.Error;
        }
        /// <inheritdoc/>
        public async Task<DokanAsyncResult> SetFileAttributes(string filename, FileAttributes attributes, AsyncDokanFileInfo info)
        {
            JS.Log($"SetFileAttributes: {info.OpId} {filename} {attributes}", info);

            return DokanResult.NotImplemented;
        }
        /// <inheritdoc/>
        public async Task<DokanAsyncResult> SetFileTime(string filename, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, AsyncDokanFileInfo info)
        {
            JS.Log($"SetFileTime: {info.OpId} {filename} {creationTime} {lastAccessTime} {lastWriteTime}", info);

            return DokanResult.NotImplemented;
        }
        /// <inheritdoc/>
        public async Task<DokanAsyncResult> MoveFile(string oldName, string newName, bool replace, AsyncDokanFileInfo info)
        {
            JS.Log($"MoveFile: {info.OpId} {oldName} {newName} {replace}", info);
            // TODO - renaming a folder uses this (among other things.) this needs to be implemented
            using var srcHandle = await GetPathHandle(oldName);
            if (srcHandle is FileSystemFileHandle fHandle)
            {
                // FileSystemHandles do not currently support renaming or moving (though it is planned for the Origin private file system)
                // the data currently has to be copied to the new file
                // this is a simple read write of the data, large files would need better handling
                try
                {
                    using var fsRoot = await GetPathDirectoryHandle();
                    using var newFile = await GetPathFileHandle(newName, true);
                    using var data = await fHandle.ReadArrayBuffer();
                    await newFile!.Write(data);
                    await fsRoot!.RemovePath(oldName);
                    return DokanResult.Success;
                }
                catch { }
            }
            else if (srcHandle is FileSystemDirectoryHandle dHandle)
            {
                var isEmpty = !(await dHandle.KeysList()).Any();
                if (isEmpty)
                {
                    // moving an empty folder is a simple 'create the new and delete the old'. no content to deal with.
                    try
                    {
                        using var fsRoot = await GetPathDirectoryHandle();
                        await fsRoot!.CreatePathDirectory(newName);
                        await fsRoot!.RemovePath(oldName);
                        return DokanResult.Success;
                    }
                    catch { }
                }
                else
                {
                    // TODO
                    // Create the new directory
                    // move over files and folders (requires copying)
                    // ....
                }
            }
            return DokanResult.NotImplemented;
        }
        #region Not supported
        /// <inheritdoc/>
        public Task<GetFileSecurityResult> GetFileSecurity(string filename, AccessControlSections sections, AsyncDokanFileInfo info)
        {
            JS.Log($"GetFileSecurity: {info.OpId} {filename}", info);
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public Task<DokanAsyncResult> SetFileSecurity(string filename, FileSystemSecurity security, AccessControlSections sections, AsyncDokanFileInfo info)
        {
            JS.Log($"SetFileSecurity: {info.OpId} {filename}", info);
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public Task<FindStreamsResult> FindStreams(string filename, AsyncDokanFileInfo info)
        {
            JS.Log($"FindStreams: {info.OpId} {filename}", info);
            throw new NotImplementedException();
        }
        #endregion
        #region Drive ops
        /// <inheritdoc/>
        public Task<GetDiskFreeSpaceResult> GetDiskFreeSpace(AsyncDokanFileInfo info)
        {
            JS.Log($"GetDiskFreeSpace: {info.OpId}", info);
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public Task<GetVolumeInformationResult> GetVolumeInformation(AsyncDokanFileInfo info)
        {
            JS.Log($"GetVolumeInformation: {info.OpId}", info);
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public Task<DokanAsyncResult> Unmounted(AsyncDokanFileInfo info)
        {
            JS.Log($"Unmounted: {info.OpId}", info);
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public Task<DokanAsyncResult> Mounted(string mountPoint, AsyncDokanFileInfo info)
        {
            JS.Log($"Mounted: {info.OpId}", info);
            throw new NotImplementedException();
        }
        #endregion
    }
}
