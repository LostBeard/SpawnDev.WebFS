using DokanNet;

namespace SpawnDev.WebFS.DokanAsync
{
    public class ReadFileResult : DokanAsyncResult
    {
        public static implicit operator ReadFileResult(NtStatus status) => new ReadFileResult(status);
        public byte[]? Data { get; set; }
        public ReadFileResult() { }
        public ReadFileResult(NtStatus status, byte[]? data = null)
        {
            Status = status;
            Data = data;
        }
    }
}
