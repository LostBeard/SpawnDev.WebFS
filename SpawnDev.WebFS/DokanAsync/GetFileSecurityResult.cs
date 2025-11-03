using DokanNet;
using System.Security.AccessControl;

namespace SpawnDev.WebFS.DokanAsync
{
    /// <summary>
    /// TODO - this type needs to be tested if it can actually be serialized
    /// </summary>
    public class GetFileSecurityResult : DokanAsyncResult
    {
        public static implicit operator GetFileSecurityResult(NtStatus status) => new GetFileSecurityResult(status);
        public FileSystemSecurity? Security { get; set; }
        public GetFileSecurityResult() { }
        public GetFileSecurityResult(NtStatus status, FileSystemSecurity? security = default)
        {
            Status = status;
            Security = security;
        }
    }
}
