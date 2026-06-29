using DokanNet;
using SpawnDev.BlazorJS.JSObjects;
using System.Text.Json.Serialization;

namespace SpawnDev.WebFS.DokanAsync
{
    public class ReadFileResultJS : DokanAsyncResult
    {
        public static implicit operator ReadFileResultJS(NtStatus status) => new ReadFileResultJS(status);
        [JsonPropertyName("Data")]
        public Uint8Array? Data { get; set; }
        public ReadFileResultJS() { }
        public ReadFileResultJS(NtStatus status, Uint8Array? data = null)
        {
            Status = status;
            Data = data;
        }
    }
}
