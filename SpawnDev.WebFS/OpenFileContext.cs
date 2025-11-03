using FileOptions = System.IO.FileOptions;

namespace SpawnDev.WebFS
{
    public class OpenFileContext
    {
        public string Filename { get; set; }
        public DokanNet.FileAccess Access { get; set; }
        public FileShare Share { get; set; }
        public FileMode Mode { get; set; }
        public FileOptions Options { get; set; }
        public FileAttributes Attributes { get; set; }
        public AsyncDokanFileInfo Info { get; set; }
        public object Context { get; set; }
        public OpenFileContext(string filename, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, AsyncDokanFileInfo info)
        {
            Filename = filename;
            Access = access;
            Share = share;
            Mode = mode;
            Options = options;
            Attributes = attributes;
            Info = info;
        }
    }
}
