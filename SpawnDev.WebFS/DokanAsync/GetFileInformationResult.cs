using DokanNet;

namespace SpawnDev.WebFS.DokanAsync
{
    public class GetFileInformationResult : DokanAsyncResult
    {
        public static implicit operator GetFileInformationResult(NtStatus status) => new GetFileInformationResult(status);
        public FileInformation FileInfo { get; set; }
        public GetFileInformationResult() { }
        public GetFileInformationResult(NtStatus status, FileInformation fileInfo = default)
        {
            Status = status;
            FileInfo = fileInfo;
        }
    }
}
