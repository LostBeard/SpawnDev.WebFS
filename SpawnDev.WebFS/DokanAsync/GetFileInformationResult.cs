using DokanNet;
using System.Text.Json.Serialization;

namespace SpawnDev.WebFS.DokanAsync
{
    public class GetFileInformationResult : DokanAsyncResult
    {
        public static implicit operator GetFileInformationResult(NtStatus status) => new GetFileInformationResult(status);

        /// <summary>
        /// TODO : FileInfo may not serialize/deserialize properly due to proeprty name case case handling in MessagePack
        /// </summary>
        [JsonPropertyName("FileInfo")]
        public FileInformationClass FileInfo { get; set; }
        public GetFileInformationResult() { }
        public GetFileInformationResult(NtStatus status, FileInformationClass fileInfo = default)
        {
            Status = status;
            FileInfo = fileInfo;
        }
    }
}
