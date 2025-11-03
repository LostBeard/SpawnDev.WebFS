using DokanNet;
using System.Security.AccessControl;
using SpawnDev.WebFS;
using FileAccess = DokanNet.FileAccess;

namespace SpawnDev.WebFS.DokanAsync
{
    public interface IAsyncDokanOperations
    {
        //
        // Summary:
        //     CreateFile is called each time a request is made on a file system object. In
        //     case mode is System.IO.FileMode.OpenOrCreate and System.IO.FileMode.Create and
        //     CreateFile are successfully opening a already existing file, you have to return
        //     DokanNet.DokanAsyncResult.AlreadyExists instead of DokanNet.NtStatus.Success. If the
        //     file is a directory, CreateFile is also called. In this case, CreateFile should
        //     return DokanNet.NtStatus.Success when that directory can be opened and DokanNet.AsyncDokanFileInfo.IsDirectory
        //     must be set to true. On the other hand, if DokanNet.AsyncDokanFileInfo.IsDirectory
        //     is set to true but the path target a file, you need to return DokanNet.DokanAsyncResult.NotADirectory
        //     DokanNet.AsyncDokanFileInfo.Context can be used to store data (like System.IO.FileStream)
        //     that can be retrieved in all other request related to the context.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   access:
        //     A DokanNet.FileAccess with permissions for file or directory.
        //
        //   share:
        //     Type of share access to other threads, which is specified as System.IO.FileShare.None
        //     or any combination of System.IO.FileShare. Device and intermediate drivers usually
        //     set ShareAccess to zero, which gives the caller exclusive access to the open
        //     file.
        //
        //   mode:
        //     Specifies how the operating system should open a file. See FileMode Enumeration
        //     (MSDN).
        //
        //   options:
        //     Represents advanced options for creating a FileStream object. See FileOptions
        //     Enumeration (MSDN).
        //
        //   attributes:
        //     Provides attributes for files and directories. See FileAttributes Enumeration
        //     (MSDN>.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<CreateFileResult> CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Receipt of this request indicates that the last handle for a file object that
        //     is associated with the target device object has been closed (but, due to outstanding
        //     I/O requests, might not have been released). Cleanup is requested before DokanNet.IDokanOperations.CloseFile(System.String,DokanNet.AsyncDokanFileInfo)
        //     is called.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Remarks:
        //     When DokanNet.AsyncDokanFileInfo.DeleteOnClose is true, you must delete the file
        //     in Cleanup. Refer to DokanNet.IDokanOperations.DeleteFile(System.String,DokanNet.AsyncDokanFileInfo)
        //     and DokanNet.IDokanOperations.DeleteDirectory(System.String,DokanNet.AsyncDokanFileInfo)
        //     for explanation.
        Task Cleanup(string filename, AsyncDokanFileInfo info);

        //
        // Summary:
        //     CloseFile is called at the end of the life of the context. Receipt of this request
        //     indicates that the last handle of the file object that is associated with the
        //     target device object has been closed and released. All outstanding I/O requests
        //     have been completed or canceled. CloseFile is requested after DokanNet.IDokanOperations.Cleanup(System.String,DokanNet.AsyncDokanFileInfo)
        //     is called. Remainings in DokanNet.AsyncDokanFileInfo.Context has to be cleared before
        //     return.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        Task CloseFile(string filename, AsyncDokanFileInfo info);

        //
        // Summary:
        //     ReadFile callback on the file previously opened in DokanNet.IDokanOperations.CreateFile(System.String,DokanNet.FileAccess,System.IO.FileShare,System.IO.FileMode,System.IO.FileOptions,System.IO.FileAttributes,DokanNet.AsyncDokanFileInfo).
        //     It can be called by different thread at the same time, therefor the read has
        //     to be thread safe.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   buffer:
        //     Read buffer that has to be fill with the read result. The buffer size depend
        //     of the read size requested by the kernel.
        //
        //   bytesRead:
        //     Total number of bytes that has been read.
        //
        //   offset:
        //     Offset from where the read has to be proceed.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<ReadFileResult> ReadFile(string filename, long offset, long maxCount, AsyncDokanFileInfo info);

        //
        // Summary:
        //     WriteFile callback on the file previously opened in DokanNet.IDokanOperations.CreateFile(System.String,DokanNet.FileAccess,System.IO.FileShare,System.IO.FileMode,System.IO.FileOptions,System.IO.FileAttributes,DokanNet.AsyncDokanFileInfo)
        //     It can be called by different thread at the same time, therefor the write/context
        //     has to be thread safe.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   buffer:
        //     Data that has to be written.
        //
        //   bytesWritten:
        //     Total number of bytes that has been write.
        //
        //   offset:
        //     Offset from where the write has to be proceed.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<WriteFileResult> WriteFile(string filename, byte[] buffer, long offset, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Clears buffers for this context and causes any buffered data to be written to
        //     the file.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<DokanAsyncResult> FlushFileBuffers(string filename, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Get specific informations on a file.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   fileInfo:
        //     DokanNet.FileInformation struct to fill
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<GetFileInformationResult> GetFileInformation(string filename, AsyncDokanFileInfo info);

        //
        // Summary:
        //     List all files in the path requested DokanNet.IDokanOperations.FindFilesWithPattern(System.String,System.String,System.Collections.Generic.IList{DokanNet.FileInformation}@,DokanNet.AsyncDokanFileInfo)
        //     is checking first. If it is not implemented or returns DokanNet.NtStatus.NotImplemented,
        //     then FindFiles is called.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   files:
        //     A list of DokanNet.FileInformation to return.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<FindFilesResult> FindFiles(string filename, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Same as DokanNet.IDokanOperations.FindFiles(System.String,System.Collections.Generic.IList{DokanNet.FileInformation}@,DokanNet.AsyncDokanFileInfo)
        //     but with a search pattern to filter the result.
        //
        // Parameters:
        //   filename:
        //     Path requested by the Kernel on the FileSystem.
        //
        //   searchPattern:
        //     Search pattern
        //
        //   files:
        //     A list of DokanNet.FileInformation to return.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<FindFilesResult> FindFilesWithPattern(string filename, string searchPattern, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Set file attributes on a specific file.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   attributes:
        //     System.IO.FileAttributes to set on file
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        //
        //
        // Remarks:
        //     SetFileAttributes and DokanNet.IDokanOperations.SetFileTime(System.String,System.Nullable{System.DateTime},System.Nullable{System.DateTime},System.Nullable{System.DateTime},DokanNet.AsyncDokanFileInfo)
        //     are called only if both of them are implemented.
        Task<DokanAsyncResult> SetFileAttributes(string filename, FileAttributes attributes, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Set file times on a specific file. If System.DateTime is null, this should not
        //     be updated.
        //
        // Parameters:
        //   filename:
        //     File or directory name.
        //
        //   creationTime:
        //     System.DateTime when the file was created.
        //
        //   lastAccessTime:
        //     System.DateTime when the file was last accessed.
        //
        //   lastWriteTime:
        //     System.DateTime when the file was last written to.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        //
        //
        // Remarks:
        //     DokanNet.IDokanOperations.SetFileAttributes(System.String,System.IO.FileAttributes,DokanNet.AsyncDokanFileInfo)
        //     and SetFileTime are called only if both of them are implemented.
        Task<DokanAsyncResult> SetFileTime(string filename, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Check if it is possible to delete a file.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     Return DokanNet.DokanAsyncResult.Success if file can be delete or DokanNet.NtStatus
        //     appropriate.
        //
        // Remarks:
        //     You should NOT delete the file in DeleteFile, but instead you must only check
        //     whether you can delete the file or not, and return DokanNet.NtStatus.Success
        //     (when you can delete it) or appropriate error codes such as DokanNet.NtStatus.AccessDenied,
        //     DokanNet.NtStatus.ObjectNameNotFound. DeleteFile will also be called with DokanNet.AsyncDokanFileInfo.DeleteOnClose
        //     set to false to notify the driver when the file is no longer requested to be
        //     deleted. When you return DokanNet.NtStatus.Success, you get a DokanNet.IDokanOperations.Cleanup(System.String,DokanNet.AsyncDokanFileInfo)
        //     call afterwards with DokanNet.AsyncDokanFileInfo.DeleteOnClose set to true and only
        //     then you have to actually delete the file being closed.
        Task<DokanAsyncResult> DeleteFile(string filename, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Check if it is possible to delete a directory.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     Return DokanNet.DokanAsyncResult.Success if file can be delete or DokanNet.NtStatus
        //     appropriate.
        //
        // Remarks:
        //     You should NOT delete the file in DokanNet.IDokanOperations.DeleteDirectory(System.String,DokanNet.AsyncDokanFileInfo),
        //     but instead you must only check whether you can delete the file or not, and return
        //     DokanNet.NtStatus.Success (when you can delete it) or appropriate error codes
        //     such as DokanNet.NtStatus.AccessDenied, DokanNet.NtStatus.ObjectPathNotFound,
        //     DokanNet.NtStatus.ObjectNameNotFound. DeleteFile will also be called with DokanNet.AsyncDokanFileInfo.DeleteOnClose
        //     set to false to notify the driver when the file is no longer requested to be
        //     deleted. When you return DokanNet.NtStatus.Success, you get a DokanNet.IDokanOperations.Cleanup(System.String,DokanNet.AsyncDokanFileInfo)
        //     call afterwards with DokanNet.AsyncDokanFileInfo.DeleteOnClose set to true and only
        //     then you have to actually delete the file being closed.
        Task<DokanAsyncResult> DeleteDirectory(string filename, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Move a file or directory to a new location.
        //
        // Parameters:
        //   oldName:
        //     Path to the file to move.
        //
        //   newName:
        //     Path to the new location for the file.
        //
        //   replace:
        //     If the file should be replaced if it already exist a file with path newName.
        //
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<DokanAsyncResult> MoveFile(string oldName, string newName, bool replace, AsyncDokanFileInfo info);

        //
        // Summary:
        //     SetEndOfFile is used to truncate or extend a file (physical file size).
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   length:
        //     File length to set
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<DokanAsyncResult> SetEndOfFile(string filename, long length, AsyncDokanFileInfo info);

        //
        // Summary:
        //     SetAllocationSize is used to truncate or extend a file.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   length:
        //     File length to set
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<DokanAsyncResult> SetAllocationSize(string filename, long length, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Lock file at a specific offset and data length. This is only used if DokanNet.DokanOptions.UserModeLock
        //     is enabled.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   offset:
        //     Offset from where the lock has to be proceed.
        //
        //   length:
        //     Data length to lock.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<DokanAsyncResult> LockFile(string filename, long offset, long length, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Unlock file at a specific offset and data length. This is only used if DokanNet.DokanOptions.UserModeLock
        //     is enabled.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   offset:
        //     Offset from where the unlock has to be proceed.
        //
        //   length:
        //     Data length to lock.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<DokanAsyncResult> UnlockFile(string filename, long offset, long length, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Retrieves information about the amount of space that is available on a disk volume,
        //     which is the total amount of space, the total amount of free space, and the total
        //     amount of free space available to the user that is associated with the calling
        //     thread.
        //
        // Parameters:
        //   freeBytesAvailable:
        //     Amount of available space.
        //
        //   totalNumberOfBytes:
        //     Total size of storage space.
        //
        //   totalNumberOfFreeBytes:
        //     Amount of free space.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        //
        //
        // Remarks:
        //     Neither GetDiskFreeSpace nor DokanNet.IDokanOperations.GetVolumeInformation(System.String@,DokanNet.FileSystemFeatures@,System.String@,System.UInt32@,DokanNet.AsyncDokanFileInfo)
        //     save the DokanNet.AsyncDokanFileInfo.Context. Before these methods are called, DokanNet.IDokanOperations.CreateFile(System.String,DokanNet.FileAccess,System.IO.FileShare,System.IO.FileMode,System.IO.FileOptions,System.IO.FileAttributes,DokanNet.AsyncDokanFileInfo)
        //     may not be called. (ditto DokanNet.IDokanOperations.CloseFile(System.String,DokanNet.AsyncDokanFileInfo)
        //     and DokanNet.IDokanOperations.Cleanup(System.String,DokanNet.AsyncDokanFileInfo)).
        Task<GetDiskFreeSpaceResult> GetDiskFreeSpace(AsyncDokanFileInfo info);

        //
        // Summary:
        //     Retrieves information about the file system and volume associated with the specified
        //     root directory.
        //
        // Parameters:
        //   volumeLabel:
        //     Volume name
        //
        //   features:
        //     DokanNet.FileSystemFeatures with features enabled on the volume.
        //
        //   fileSystemName:
        //     The name of the specified volume.
        //
        //   maximumComponentLength:
        //     File name component that the specified file system supports.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        //
        //
        // Remarks:
        //     Neither GetVolumeInformation nor DokanNet.IDokanOperations.GetDiskFreeSpace(System.Int64@,System.Int64@,System.Int64@,DokanNet.AsyncDokanFileInfo)
        //     save the DokanNet.AsyncDokanFileInfo.Context. Before these methods are called, DokanNet.IDokanOperations.CreateFile(System.String,DokanNet.FileAccess,System.IO.FileShare,System.IO.FileMode,System.IO.FileOptions,System.IO.FileAttributes,DokanNet.AsyncDokanFileInfo)
        //     may not be called. (ditto DokanNet.IDokanOperations.CloseFile(System.String,DokanNet.AsyncDokanFileInfo)
        //     and DokanNet.IDokanOperations.Cleanup(System.String,DokanNet.AsyncDokanFileInfo)).
        //     DokanNet.FileSystemFeatures.ReadOnlyVolume is automatically added to the features
        //     if DokanNet.DokanOptions.WriteProtection was specified when the volume was mounted.
        //     If DokanNet.NtStatus.NotImplemented is returned, the %Dokan kernel driver use
        //     following settings by default: | Parameter | Default value | |------------------------------|--------------------------------------------------------------------------------------------------|
        //     | \a rawVolumeNameBuffer | "DOKAN" | | \a rawVolumeSerialNumber | 0x19831116
        //     | | \a rawMaximumComponentLength | 256 | | \a rawFileSystemFlags | CaseSensitiveSearch
        //     \|\| CasePreservedNames \|\| SupportsRemoteStorage \|\| UnicodeOnDisk | | \a
        //     rawFileSystemNameBuffer | "NTFS" |
        Task<GetVolumeInformationResult> GetVolumeInformation(AsyncDokanFileInfo info);

        //
        // Summary:
        //     Get specified information about the security of a file or directory.
        //
        // Parameters:
        //   filename:
        //     File or directory name.
        //
        //   security:
        //     A System.Security.AccessControl.FileSystemSecurity with security information
        //     to return.
        //
        //   sections:
        //     A System.Security.AccessControl.AccessControlSections with access sections to
        //     return.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        //
        //
        // Remarks:
        //     If DokanNet.NtStatus.NotImplemented is returned, dokan library will handle the
        //     request by building a sddl of the current process user with authenticate user
        //     rights for context menu.
        Task<GetFileSecurityResult> GetFileSecurity(string filename, AccessControlSections sections, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Sets the security of a file or directory object.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   security:
        //     A System.Security.AccessControl.FileSystemSecurity with security information
        //     to set.
        //
        //   sections:
        //     A System.Security.AccessControl.AccessControlSections with access sections on
        //     which.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<DokanAsyncResult> SetFileSecurity(string filename, FileSystemSecurity security, AccessControlSections sections, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Is called when %Dokan succeed to mount the volume. If DokanNet.DokanOptions.MountManager
        //     is enabled and the drive letter requested is busy, the mountPoint can contain
        //     a different drive letter that the mount manager assigned us.
        //
        // Parameters:
        //   mountPoint:
        //     The mount point assign to the instance.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<DokanAsyncResult> Mounted(string mountPoint, AsyncDokanFileInfo info);

        //
        // Summary:
        //     Is called when %Dokan is unmounting the volume.
        //
        // Parameters:
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        Task<DokanAsyncResult> Unmounted(AsyncDokanFileInfo info);

        //
        // Summary:
        //     Retrieve all NTFS Streams informations on the file. This is only called if DokanNet.DokanOptions.AltStream
        //     is enabled.
        //
        // Parameters:
        //   filename:
        //     File path requested by the Kernel on the FileSystem.
        //
        //   streams:
        //     List of DokanNet.FileInformation for each streams present on the file.
        //
        //   info:
        //     An DokanNet.AsyncDokanFileInfo with information about the file or directory.
        //
        // Returns:
        //     Return DokanNet.NtStatus or DokanNet.DokanAsyncResult appropriate to the request result.
        //
        //
        // Remarks:
        //     For files, the first item in streams is information about the default data stream
        //     "::$DATA".
        Task<FindStreamsResult> FindStreams(string filename, AsyncDokanFileInfo info);
    }
}
