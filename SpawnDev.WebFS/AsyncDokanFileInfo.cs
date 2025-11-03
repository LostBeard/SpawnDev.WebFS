using DokanNet;

namespace SpawnDev.WebFS
{
    public class AsyncDokanFileInfo
    {
        public static AsyncDokanFileInfo From(IDokanFileInfo info)
        {
            var op = info.Context is AsyncDokanFileInfo adi ? adi : null;
            if (op == null)
            {
                op = new AsyncDokanFileInfo
                {
                    // 
                    WriteToEndOfFile = info.WriteToEndOfFile,
                    IsDirectory = info.IsDirectory,
                    DeleteOnClose = info.DeleteOnClose,// info.DeletePending,
                    OpId = op?.OpId ?? Guid.NewGuid().ToString(),
                };
                info.Context = op;
            }
            return op;
        }
        public bool WriteToEndOfFile { get; set; }
        public bool IsDirectory { get; set; }
        public bool DeleteOnClose { get; set; }
        public string OpId { get; init; }
    }
}
