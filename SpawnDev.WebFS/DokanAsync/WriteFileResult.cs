using DokanNet;
using System.Text.Json.Serialization;

namespace SpawnDev.WebFS.DokanAsync
{
    public class WriteFileResult : DokanAsyncResult
    {
        public static implicit operator WriteFileResult(NtStatus status) => new WriteFileResult(status);
        [JsonPropertyName("BytesWritten")]
        public int BytesWritten { get; set; }
        public WriteFileResult() { }
        public WriteFileResult(NtStatus status, int bytesWritten = 0)
        {
            Status = status;
            BytesWritten = bytesWritten;
        }
        public WriteFileResult(int bytesWritten)
        {
            Status = NtStatus.Success;
            BytesWritten = bytesWritten;
        }
    }
}
