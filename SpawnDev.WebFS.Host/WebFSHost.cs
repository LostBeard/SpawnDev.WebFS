using DokanNet;
using DokanNet.Logging;
using SpawnDev.BlazorJS;
using SpawnDev.DB;
using SpawnDev.WebFS.DokanAsync;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SpawnDev.WebFS.Host
{
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
                MountPoint = $@"{available.Last()}:\";
                return;
            }
            throw new Exception("No drive letter available.");
        }
        public static List<char> FindUnusedDriveLetters()
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
            List<char> unusedDriveLetters = allPossibleDriveLetters
                                            .Except(usedDriveLetters)
                                            .ToList();

            return unusedDriveLetters;
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
