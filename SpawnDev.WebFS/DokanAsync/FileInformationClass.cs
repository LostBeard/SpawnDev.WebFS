using DokanNet;
using System.Text.Json.Serialization;

namespace SpawnDev.WebFS.DokanAsync
{
    public class FileInformationClass
    {
        public static implicit operator FileInformation?(FileInformationClass value) => value == null ? null : new FileInformation
        {
            Attributes = value.Attributes,
            CreationTime = value.CreationTime == null ? null : new DateTime(value.CreationTime.Value, DateTimeKind.Utc),
            FileName = value.FileName,
            LastAccessTime = value.LastAccessTime == null ? null : new DateTime(value.LastAccessTime.Value, DateTimeKind.Utc),
            LastWriteTime = value.LastWriteTime == null ? null : new DateTime(value.LastWriteTime.Value, DateTimeKind.Utc),
            Length = value.Length,
        };
        public static implicit operator FileInformation(FileInformationClass value) => new FileInformation
        {
            Attributes = value.Attributes,
            CreationTime = value.CreationTime == null ? null : new DateTime(value.CreationTime.Value, DateTimeKind.Utc),
            FileName = value.FileName,
            LastAccessTime = value.LastAccessTime == null ? null : new DateTime(value.LastAccessTime.Value, DateTimeKind.Utc),
            LastWriteTime = value.LastWriteTime == null ? null : new DateTime(value.LastWriteTime.Value, DateTimeKind.Utc),
            Length = value.Length,
        };
        public static implicit operator FileInformationClass(FileInformation value) => new FileInformationClass
        {
            Attributes = value.Attributes,
            CreationTime = value.CreationTime == null ? null : value.CreationTime.Value.Ticks,
            FileName = value.FileName,
            LastAccessTime = value.LastAccessTime == null ? null : value.LastAccessTime.Value.Ticks,
            LastWriteTime = value.LastWriteTime == null ? null : value.LastWriteTime.Value.Ticks,
            Length = value.Length,
        };
        public static implicit operator FileInformationClass(FileInformation? value1)
        {
            if (value1 == null) return null;
            var value = value1.Value;
            return new FileInformationClass
               {

                   Attributes = value.Attributes,
                   CreationTime = value.CreationTime == null ? null : value.CreationTime.Value.Ticks,
                   FileName = value.FileName,
                   LastAccessTime = value.LastAccessTime == null ? null : value.LastAccessTime.Value.Ticks,
                   LastWriteTime = value.LastWriteTime == null ? null : value.LastWriteTime.Value.Ticks,
                   Length = value.Length,
               };
        }
        //
        // Summary:
        //     Gets or sets the name of the file or directory. DokanNet.IDokanOperations.GetFileInformation(System.String,DokanNet.FileInformation@,DokanNet.IDokanFileInfo)
        //     required the file path when other operations only need the file name.
        [JsonPropertyName("FileName")]
        public string FileName { get; set; }

        //
        // Summary:
        //     Gets or sets the System.IO.FileAttributes for the file or directory.
        [JsonPropertyName("Attributes")]
        public FileAttributes Attributes { get; set; }

        //
        // Summary:
        //     Gets or sets the creation time of the file or directory. If equal to null, the
        //     value will not be set or the file has no creation time.
        [JsonPropertyName("CreationTime")]
        public long? CreationTime { get; set; }

        //
        // Summary:
        //     Gets or sets the last access time of the file or directory. If equal to null,
        //     the value will not be set or the file has no last access time.
        [JsonPropertyName("LastAccessTime")]
        public long? LastAccessTime { get; set; }

        //
        // Summary:
        //     Gets or sets the last write time of the file or directory. If equal to null,
        //     the value will not be set or the file has no last write time.
        [JsonPropertyName("LastWriteTime")]
        public long? LastWriteTime { get; set; }

        //
        // Summary:
        //     Gets or sets the length of the file.
        [JsonPropertyName("Length")]
        public long Length { get; set; }
    }
}
