using Microsoft.Extensions.DependencyInjection;
using SpawnDev.DB;
using SpawnDev.WebFS.Host;

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
            using var mutex = new Mutex(false, "{425EADEC-F048-476E-8977-DC4D78DF48A1}");
            if (!mutex.WaitOne(1))
            {
                // another instance is running. give it a chance to close, it may have started this process as a restart.
                Thread.Sleep(1000); 
                if (!mutex.WaitOne(1)) return; // another instance is still running. exit.
            }
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