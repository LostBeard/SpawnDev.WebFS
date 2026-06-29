using DokanNet;
using System.Text.Json.Serialization;

namespace SpawnDev.WebFS.DokanAsync
{
    public class FindFilesResult : DokanAsyncResult
    {
        public static implicit operator FindFilesResult(NtStatus status) => new FindFilesResult(status);


        /// <summary>
        /// TODO : Files may not serialize/deserialize properly
        /// </summary>
        [JsonPropertyName("Files")]
        public List<FileInformationClass>? Files { get; set; }
        public FindFilesResult() { }
        public FindFilesResult(NtStatus status, List<FileInformationClass>? files = default)
        {
            Status = status;
            Files = files;
        }
        //public FindFilesResult(NtStatus status, List<FileInformation> files)
        //{
        //    Status = status;
        //    Files = files?.Select(o => (FileInformationClass)o).ToList();
        //}
    }
}
