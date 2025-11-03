using DokanNet;

namespace SpawnDev.WebFS.DokanAsync
{
    public class GetDiskFreeSpaceResult : DokanAsyncResult
    {
        public static implicit operator GetDiskFreeSpaceResult(NtStatus status) => new GetDiskFreeSpaceResult(status);
        public long FreeBytesAvailable { get; set; }
        public long TotalNumberOfBytes { get; set; }
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
