using Microsoft.Extensions.DependencyInjection;
using SpawnDev.DB;
using SpawnDev.WebFS.Host;
using System.Diagnostics;

namespace SpawnDev.WebFS.Tray
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task Main(string[] args)
        {
            var tp = Process.GetCurrentProcess();
            var p = Process.GetProcessesByName(tp.ProcessName);
            var cnt = p.Length;
            if (cnt > 1) Thread.Sleep(1000); // give running app chance to close
            using var mutex = new Mutex(false, "{425EADEC-F048-476E-8977-DC4D78DF48A1}");
            if (!mutex.WaitOne(1)) return; // another instance is running
            var host = await InitApp(args);
            ApplicationConfiguration.Initialize();
            Application.Run(new frmMain(host));
            mutex.ReleaseMutex();
        }
        static async Task<WinFormsApp> InitApp(string[] args)
        {
            var builder = WinFormsAppBuilder.CreateDefault(args);
            // Background service manager for auto-started services
            builder.Services.AddBackgroundServiceManager();
            // AppDB (Sqlite application database)
            builder.Services.AddAppDB();
            // WebFSServer and WebFSHost
            builder.Services.AddSingleton<WebFSServer>();
            builder.Services.AddSingleton<WebFSHost>();
            // Build
            var host = builder.Build();
            // Start background services
            await host.Services.StartBackgroundServices();
            return host;
        }
    }
}