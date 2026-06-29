using DokanNet;
using System.Text.Json.Serialization;

namespace SpawnDev.WebFS.DokanAsync
{
    public class FindStreamsResult : DokanAsyncResult
    {
        public static implicit operator FindStreamsResult(NtStatus status) => new FindStreamsResult(status);


        /// <summary>
        /// TODO : Streams may not serialize/deserialize properly
        /// </summary>
        [JsonPropertyName("Streams")]
        public List<FileInformationClass>? Streams { get; set; }
        public FindStreamsResult() { }
        public FindStreamsResult(NtStatus status, List<FileInformationClass>? streams = default)
        {
            Status = status;
            Streams = streams;
        }
    }
}
