using DokanNet;

namespace SpawnDev.WebFS.DokanAsync
{
    public class CreateFileResult : DokanAsyncResult
    {
        public static implicit operator CreateFileResult(NtStatus status) => new CreateFileResult(status);
        public bool IsDirectory { get; set; }
        public CreateFileResult() { }
        public CreateFileResult(NtStatus status, bool isDirectory = false)
        {
            Status = status;
            IsDirectory = isDirectory;
        }
    }
    public class FindFilesResult : DokanAsyncResult
    {
        public static implicit operator FindFilesResult(NtStatus status) => new FindFilesResult(status);
        public IList<FileInformation>? Files { get; set; }
        public FindFilesResult() { }
        public FindFilesResult(NtStatus status, IList<FileInformation>? files = default)
        {
            Status = status;
            Files = files;
        }
    }
}
