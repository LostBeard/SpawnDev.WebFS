using DokanNet;

namespace SpawnDev.WebFS.DokanAsync
{
    public class GetVolumeInformationResult : DokanAsyncResult
    {
        public static implicit operator GetVolumeInformationResult(NtStatus status) => new GetVolumeInformationResult(status);
        public string VolumeLabel { get; set; } = "";
        public FileSystemFeatures Features { get; set; } = FileSystemFeatures.None;
        public string FileSystemName { get; set; } = "";
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
