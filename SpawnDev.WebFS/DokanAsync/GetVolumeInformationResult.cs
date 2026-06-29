using DokanNet;
using System.Text.Json.Serialization;

namespace SpawnDev.WebFS.DokanAsync
{
    public class GetVolumeInformationResult : DokanAsyncResult
    {
        public static implicit operator GetVolumeInformationResult(NtStatus status) => new GetVolumeInformationResult(status);

        [JsonPropertyName("VolumeLabel")]
        public string VolumeLabel { get; set; } = "";

        [JsonPropertyName("Features")]
        public FileSystemFeatures Features { get; set; } = FileSystemFeatures.None;

        [JsonPropertyName("FileSystemName")]
        public string FileSystemName { get; set; } = "";

        [JsonPropertyName("MaximumComponentLength")]
        public uint MaximumComponentLength { get; set; } = 256;

        public GetVolumeInformationResult() { }
        public GetVolumeInformationResult(NtStatus status, string volumeLabel = "", FileSystemFeatures features = FileSystemFeatures.None, string fileSystemName = "", uint maximumComponentLength = 256)
        {
            Status = status;
            VolumeLabel = volumeLabel;
            Features = features;
            FileSystemName = fileSystemName;
            MaximumComponentLength = maximumComponentLength;
        }
    }
}
