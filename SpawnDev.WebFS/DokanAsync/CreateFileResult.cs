using DokanNet;
using System.Text.Json.Serialization;

namespace SpawnDev.WebFS.DokanAsync
{
    public class CreateFileResult : DokanAsyncResult
    {
        public static implicit operator CreateFileResult(NtStatus status) => new CreateFileResult(status);

        [JsonPropertyName("IsDirectory")]
        public bool IsDirectory { get; set; }
        public CreateFileResult() { }
        public CreateFileResult(NtStatus status, bool isDirectory = false)
        {
            Status = status;
            IsDirectory = isDirectory;
        }
    }
}
