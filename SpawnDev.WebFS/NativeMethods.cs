using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SpawnDev.WebFS
{
    /// <summary>
    /// Basic process info
    /// </summary>
    public class BasicProcessInfo
    {
        /// <summary>
        /// Process path (may be null)
        /// </summary>
        public string? Path { get; set; }
        /// <summary>
        /// Process filename
        /// </summary>
        public string? FileName { get; set; }
        /// <summary>
        /// Process id
        /// </summary>
        public int Id { get; set; }
    }
    public static class NativeMethods
    {
        public static BasicProcessInfo GetProcessInfo(int processId)
        {
            var ret = new BasicProcessInfo { Id = processId, };
            try
            {
                Process process = Process.GetProcessById(processId);
                ret.Path = process.MainModule?.FileName;
                ret.FileName = Path.GetFileName(ret.Path);
            }
            catch { }
            if (string.IsNullOrEmpty(ret.Path))
            {
                ret.Path = GetProcessExePath(processId);
                ret.FileName = Path.GetFileName(ret.Path);
            }
            return ret;
        }
        public static string? GetProcessExePath(int processId)
        {
            Process? process = null;
            try
            {
                // Open the process with limited query access
                process = Process.GetProcessById(processId);
                IntPtr hProcess = process.Handle;
                uint bufferSize = 1024; // Initial buffer size
                StringBuilder sb = new StringBuilder((int)bufferSize);
                if (NativeMethods.QueryFullProcessImageName(hProcess, 0, sb, ref bufferSize))
                {
                    return sb.ToString();
                }
            }
            catch { }
            finally
            {
                process?.Dispose(); // Dispose the Process object
            }
            return null;
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        // Drive Types constants
        private const int DRIVE_UNKNOWN = 0;
        private const int DRIVE_NO_ROOT_DIR = 1; // Indicator for disconnected network drive
        private const int DRIVE_REMOVABLE = 2;
        private const int DRIVE_FIXED = 3;
        private const int DRIVE_REMOTE = 4;
        private const int DRIVE_CDROM = 5;
        private const int DRIVE_RAMDISK = 6;

        /// <summary>
        /// Retrieves the drive type.
        /// </summary>
        /// <param name="lpRootPathName">A null-terminated string that specifies the root directory.</param>
        /// <returns>The drive type constant.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetDriveType(string lpRootPathName);

        // Helper to check for the disconnected state
        public static bool IsDisconnectedNetworkDrive(string driveLetter)
        {
            // GetDriveType expects the root path format, e.g., "Z:\\"
            string rootPath = driveLetter.ToUpper() + @":\\";

            // DRIVE_NO_ROOT_DIR is the key indicator for a logically mapped but disconnected drive
            return GetDriveType(rootPath) == DRIVE_NO_ROOT_DIR;
        }
    }
}
