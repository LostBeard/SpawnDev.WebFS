using System.Security.AccessControl;
using FileAccess = DokanNet.FileAccess;

namespace SpawnDev.WebFS.DokanAsync
{
    public interface IAsyncDokanOperations : IAsyncDokanOperationsBase
    {
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
        Task<byte[]?> ReadFile(string filename, long offset, long maxCount, AsyncDokanFileInfo info);

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
    }
}
