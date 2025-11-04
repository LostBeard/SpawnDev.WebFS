using DokanNet;
using DokanNet.Logging;
using SpawnDev.BlazorJS;
using SpawnDev.DB;
using SpawnDev.WebFS.DokanAsync;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

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
    public class WebFSHost : IAsyncBackgroundService, IDisposable
    {
        public Task Ready => _Ready ??= InitAsync();
        Task? _Ready = null;
        WebFSServer WebFSServer;
        public string MountPoint
        {
            get => _MountPoint;
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
                var m = Regex.Match(value, "^([a-zA-Z])");
                if (m.Success)
                {
                    _MountPoint = m.Groups[1].Value.ToLowerInvariant() + @":\";
                }
            }
        }
        string _MountPoint = @"";
        ConsoleLogger? dokanLogger;
        Dokan? dokan;
        DokanInstance? dokanInstance;
        AppDB AppDB;
        public WebFSHost(AppDB appDB, WebFSServer webFSServer)
        {
            AppDB = appDB;
            WebFSServer = webFSServer;
        }
        void FindMountPoint()
        {
            var available = FindUnusedDriveLetters();
            var lastMountPoint = AppDB.GetSetting<string?>(nameof(MountPoint));
            if (!string.IsNullOrEmpty(lastMountPoint) && lastMountPoint.Contains(lastMountPoint[0], StringComparison.OrdinalIgnoreCase))
            {
                MountPoint = lastMountPoint;
                return;
            }
            if (available.Any())
            {
                var count = available.Count;
                if(count  > 2)
                {
                    available  = available.Take(count / 2).ToList();
                    MountPoint = $@"{available.Last()}:\";
                }
                MountPoint = $@"{available.Last()}:\";
                return;
            }
            throw new Exception("No drive letter available.");
        }
        public static List<string> FindUnusedDriveLetters()
        {
            // Get all possible drive letters (A-Z)
            List<char> allPossibleDriveLetters = Enumerable.Range('A', 'Z' - 'A' + 1)
                                                        .Select(c => (char)c)
                                                        .ToList();

            // Get currently used logical drive letters
            List<char> usedDriveLetters = DriveInfo.GetDrives()
                                                .Select(d => d.Name[0]) // Extract the drive letter from "C:\"
                                                .ToList();


            // Find the letters present in allPossibleDriveLetters but not in usedDriveLetters
            List<string> unusedDriveLetters = allPossibleDriveLetters
                                            .Except(usedDriveLetters).Select(o => $"{o}")
                                            .ToList();
            var disconnectedDrives = GetDisconnectedNetworkDriveLetters();
            unusedDriveLetters = unusedDriveLetters.Except(disconnectedDrives).ToList();
            return unusedDriveLetters;
        }
        public static List<string> GetDisconnectedNetworkDriveLetters()
        {
            List<string> disconnectedDrives = new List<string>();
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT DeviceID FROM Win32_LogicalDisk WHERE DriveType = 4");
                var mappedDriveLetters = new List<string>();
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    // WMI returns "Z:" for the drive letter
                    var driveLetter = queryObj["DeviceID"].ToString();
                    mappedDriveLetters.Add(driveLetter!.Replace(":", "")); // Extract just the letter (e.g., "Z")
                }
                // 2. Check the connection status of each mapped drive using P/Invoke
                foreach (var driveLetter in mappedDriveLetters)
                {
                    if (NativeMethods.IsDisconnectedNetworkDrive(driveLetter))
                    {
                        disconnectedDrives.Add(driveLetter);
                    }
                }
            }
            catch (ManagementException ex)
            {
                // Handle WMI specific errors (e.g., System.Management reference missing)
                System.Console.WriteLine($"WMI Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Handle WMI specific errors (e.g., System.Management reference missing)
                System.Console.WriteLine($"WMI Error: {ex.Message}");
            }
            return disconnectedDrives;
        }
        public void OpenMountPoint()
        {
            OpenFolderInExplorer(MountPoint);
        }
        public void OpenHostFolder(DomainProvider provider)
        {
            var path = Path.Combine(MountPoint, provider.Host);
            OpenFolderInExplorer(path);
        }
        public string GetHostFolder(DomainProvider provider)
        {
            var path = Path.Combine(MountPoint, provider.Host);
            return path;
        }
        public bool GetHostFolderExists(DomainProvider provider)
        {
            var path = Path.Combine(MountPoint, provider.Host);
            return Directory.Exists(path);
        }
        public void OpenHostUrl(DomainProvider provider)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = provider.Url,
                    UseShellExecute = true
                });
            }
            catch { }
        }
        public void OpenFolderInExplorer(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                try
                {
                    Process.Start("explorer.exe", folderPath);
                }
                catch { }
            }
        }
        async Task InitAsync()
        {
            await Task.Delay(100);
            _ = Task.Run(async () =>
            {
                await Task.Delay(100);
                var threadId = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"Starting DokanService ThreadId: {threadId}");
                StartIt();
            });
        }
        public void StartIt()
        {
            try
            {
                FindMountPoint();
                dokanLogger = new ConsoleLogger("[Dokan] ");
                dokan = new Dokan(dokanLogger);
                var dokanBuilder = new DokanInstanceBuilder(dokan)
                    .ConfigureOptions(options =>
                    {
                        //options.Options = DokanOptions.StderrOutput;
                        options.MountPoint = MountPoint;
                    });
                dokanInstance = dokanBuilder.Build(WebFSServer);
                Console.WriteLine(@"Success");
                AppDB.SetSetting<string?>(nameof(MountPoint), MountPoint);
                return;
            }
            catch (DokanException ex)
            {
                Console.WriteLine(@"Error: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Verify Dokan 2.3.1.1000 or later is installed and try again. Error: " + ex.Message);
            }
            // failed. cleanup.
            StopIt();
        }
        public void StopIt()
        {
            if (dokanInstance != null)
            {
                dokanInstance.Dispose();
            }
            if (dokan != null)
            {
                dokan.Dispose();
            }
            if (dokanLogger != null)
            {
                dokanLogger.Dispose();
            }
        }
        public void Dispose()
        {
            StopIt();
        }
    }
}
