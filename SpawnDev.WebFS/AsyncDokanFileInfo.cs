using DokanNet;

namespace SpawnDev.WebFS
{
    public class AsyncDokanFileInfo
    {
        public static AsyncDokanFileInfo From(IDokanFileInfo info)
        {
            var opId = (info.Context ??= Guid.NewGuid().ToString()) as string;
            var op = new AsyncDokanFileInfo
            {
                // 
                IsDirectory = info.IsDirectory,
                DeleteOnClose = info.DeleteOnClose,// info.DeletePending,
                OpId = opId!,
                WriteToEndOfFile = info.WriteToEndOfFile,
                //NoCache = info.NoCache,
                //PagingIo = info.PagingIo,
                //SynchronousIo = info.SynchronousIo,
            };
            return op;
        }

        /// <summary>
        /// Handle Id
        /// </summary>
        public string OpId { get; init; }

        // Summary:
        //     Gets or sets a value indicating whether the file has to be delete during the
        //     DokanNet.IDokanOperations.Cleanup(System.String,DokanNet.IDokanFileInfo) event.
        public bool DeleteOnClose { get; set; }

        //
        // Summary:
        //     Gets or sets a value indicating whether it requesting a directory file. Must
        //     be set in DokanNet.IDokanOperations.CreateFile(System.String,DokanNet.FileAccess,System.IO.FileShare,System.IO.FileMode,System.IO.FileOptions,System.IO.FileAttributes,DokanNet.IDokanFileInfo)
        //     if the file appear to be a folder.
        public bool IsDirectory { get; set; }

        //
        // Summary:
        //     If true, write to the current end of file instead of using the Offset parameter.
        public bool WriteToEndOfFile { get; set; }

        ////
        //// Summary:
        ////     Read or write directly from data source without cache.
        //public bool NoCache { get; init; }

        ////
        //// Summary:
        ////     Read or write is paging IO.
        //public bool PagingIo { get; init; }

        ////
        //// Summary:
        ////     Process id for the thread that originally requested a given I/O operation.
        //public int ProcessId { get; init; }

        ////
        //// Summary:
        ////     Read or write is synchronous IO.
        //public bool SynchronousIo { get; init; }
    }
}
