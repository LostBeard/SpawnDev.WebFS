using DokanNet;

namespace SpawnDev.WebFS.DokanAsync
{
    public class FindStreamsResult : DokanAsyncResult
    {
        public static implicit operator FindStreamsResult(NtStatus status) => new FindStreamsResult(status);
        public IList<FileInformation>? Streams { get; set; }
        public FindStreamsResult() { }
        public FindStreamsResult(NtStatus status, IList<FileInformation>? streams = default)
        {
            Status = status;
            Streams = streams;
        }
    }
}
