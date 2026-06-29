using DokanNet;
using System.Security.AccessControl;
using System.Text.Json.Serialization;

namespace SpawnDev.WebFS.DokanAsync
{
    public class FileSystemSecurityClass
    {
        public static implicit operator FileSystemSecurity(FileSystemSecurityClass value) => null;
        public static implicit operator FileSystemSecurityClass(FileSystemSecurity value) => null;
    }
    /// <summary>
    /// TODO - this type needs to be tested if it can actually be serialized
    /// </summary>
    public class GetFileSecurityResult : DokanAsyncResult
    {
        public static implicit operator GetFileSecurityResult(NtStatus status) => new GetFileSecurityResult(status);
        /// <summary>
        /// TODO : FileSystemSecurity may not serialize/deserialize properly due to proeprty name case case handling in MessagePack
        /// </summary>
        [JsonPropertyName("Security")]
        public FileSystemSecurityClass? Security { get; set; }
        public GetFileSecurityResult() { }
        public GetFileSecurityResult(NtStatus status, FileSystemSecurity? security = default)
        {
            Status = status;
            Security = security;
        }
    }
}
