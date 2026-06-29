using DokanNet;
using System.Text.Json.Serialization;

namespace SpawnDev.WebFS.DokanAsync
{
    public class GetDiskFreeSpaceResult : DokanAsyncResult
    {
        public static implicit operator GetDiskFreeSpaceResult(NtStatus status) => new GetDiskFreeSpaceResult(status);

        [JsonPropertyName("FreeBytesAvailable")]
        public long FreeBytesAvailable { get; set; }

        [JsonPropertyName("TotalNumberOfBytes")]
        public long TotalNumberOfBytes { get; set; }

        [JsonPropertyName("TotalNumberOfFreeBytes")]
        public long TotalNumberOfFreeBytes { get; set; }

        public GetDiskFreeSpaceResult() { }
        public GetDiskFreeSpaceResult(NtStatus status, long freeBytesAvailable = 0, long totalNumberOfBytes = 0, long totalNumberOfFreeBytes = 0)
        {
            Status = status;
            FreeBytesAvailable = freeBytesAvailable;
            TotalNumberOfBytes = totalNumberOfBytes;
            TotalNumberOfFreeBytes = totalNumberOfFreeBytes;
        }
    }
}
