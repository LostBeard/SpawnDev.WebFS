using DokanNet;

namespace SpawnDev.WebFS.DokanAsync
{
    public class DokanAsyncResult
    {
        public static implicit operator DokanAsyncResult(NtStatus status) => new DokanAsyncResult(status);
        public NtStatus Status { get; set; }
        public DokanAsyncResult() { }
        public DokanAsyncResult(NtStatus status)
        {
            Status = status;
        }
    }
}
