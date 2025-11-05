using System.Runtime.InteropServices;

namespace SpawnDev.WebFS.Host
{
    public static class NativeMethods
    {
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
