using DokanNet;
using System.Security.AccessControl;
using FileAccess = DokanNet.FileAccess;

namespace SpawnDev.WebFS.DokanAsync
{
    public class AsyncDokanOperations : IDokanOperations
    {
        IAsyncDokanOperations Operations;
        public AsyncDokanOperations(IAsyncDokanOperations operations)
        {
            Operations = operations;
        }
        public void Cleanup(string fileName, IDokanFileInfo info)
        {
            Operations.Cleanup(fileName, AsyncDokanFileInfo.From(info)).Wait();
        }

        public void CloseFile(string fileName, IDokanFileInfo info)
        {
            Operations.CloseFile(fileName, AsyncDokanFileInfo.From(info)).Wait();
        }

        public NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, IDokanFileInfo info)
        {
            var result = Operations.CreateFile(fileName, access, share, mode, options, attributes, AsyncDokanFileInfo.From(info)).Result;
            info.IsDirectory = result.IsDirectory;
            return result.Status;
        }

        public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
        {
            var result = Operations.DeleteDirectory(fileName, AsyncDokanFileInfo.From(info)).Result;
            return result.Status;
        }

        public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
        {
            var result = Operations.DeleteFile(fileName, AsyncDokanFileInfo.From(info)).Result;
            return result.Status;
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
        {
            var result = Operations.FindFiles(fileName, AsyncDokanFileInfo.From(info)).Result;
            files = result.Files;
            return result.Status;
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
        {
            var result = Operations.FindFilesWithPattern(fileName, searchPattern, AsyncDokanFileInfo.From(info)).Result;
            files = result.Files;
            return result.Status;
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
        {
            var result = Operations.FindStreams(fileName, AsyncDokanFileInfo.From(info)).Result;
            streams = result.Streams;
            return result.Status;
        }

        public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
        {
            var result = Operations.FlushFileBuffers(fileName, AsyncDokanFileInfo.From(info)).Result;
            return result.Status;
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, IDokanFileInfo info)
        {
            var result = Operations.GetDiskFreeSpace(AsyncDokanFileInfo.From(info)).Result;
            freeBytesAvailable = result.FreeBytesAvailable;
            totalNumberOfBytes = result.TotalNumberOfBytes;
            totalNumberOfFreeBytes = result.TotalNumberOfFreeBytes;
            return result.Status;
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
        {
            var result = Operations.GetFileInformation(fileName, AsyncDokanFileInfo.From(info)).Result;
            fileInfo = result.FileInfo;
            return result.Status;
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            var result = Operations.GetFileSecurity(fileName, sections, AsyncDokanFileInfo.From(info)).Result;
            security = result.Security;
            return result.Status;
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
        {
            var result = Operations.GetVolumeInformation(AsyncDokanFileInfo.From(info)).Result;
            volumeLabel = result.VolumeLabel;
            features = result.Features;
            fileSystemName = result.FileSystemName;
            maximumComponentLength = result.MaximumComponentLength;
            return result.Status;
        }

        public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            var result = Operations.LockFile(fileName, offset, length, AsyncDokanFileInfo.From(info)).Result;
            return result.Status;
        }

        public NtStatus Mounted(string mountPoint, IDokanFileInfo info)
        {
            var result = Operations.Mounted(mountPoint, AsyncDokanFileInfo.From(info)).Result;
            return result.Status;
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
        {
            var result = Operations.MoveFile(oldName, newName, replace, AsyncDokanFileInfo.From(info)).Result;
            return result.Status;
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {
            var result = Operations.ReadFile(fileName, offset, buffer.Length, AsyncDokanFileInfo.From(info)).Result;
            bytesRead = result.Data?.Length ?? 0;
            if (result.Data != null && result.Data.Length > 0)
            {
                Buffer.BlockCopy(result.Data, 0, buffer, 0, bytesRead);
            }
            return result.Status;
        }

        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            var result = Operations.SetAllocationSize(fileName, length, AsyncDokanFileInfo.From(info)).Result;
            return result.Status;
        }

        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
        {
            var result = Operations.SetEndOfFile(fileName, length, AsyncDokanFileInfo.From(info)).Result;
            return result.Status;
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
        {
            var result = Operations.SetFileAttributes(fileName, attributes, AsyncDokanFileInfo.From(info)).Result;
            return result.Status;
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            var result = Operations.SetFileSecurity(fileName, security, sections, AsyncDokanFileInfo.From(info)).Result;
            return result.Status;
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, IDokanFileInfo info)
        {
            var result = Operations.SetFileTime(fileName, creationTime, lastAccessTime, lastWriteTime, AsyncDokanFileInfo.From(info)).Result;
            return result.Status;
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            var result = Operations.UnlockFile(fileName, offset, length, AsyncDokanFileInfo.From(info)).Result;
            return result.Status;
        }

        public NtStatus Unmounted(IDokanFileInfo info)
        {
            var result = Operations.Unmounted(AsyncDokanFileInfo.From(info)).Result;
            return result.Status;
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
        {
            var result = Operations.WriteFile(fileName, buffer, offset, AsyncDokanFileInfo.From(info)).Result;
            bytesWritten = result.BytesWritten;
            return result.Status;
        }
    }
}
