using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SpawnDev.BlazorJS;
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
            if (mutex.WaitOne(1))
            {
                var host = await InitApp(args);
                ApplicationConfiguration.Initialize();
                Application.Run(new Form1(host));
                mutex.ReleaseMutex();
            }
            mutex.Dispose();
        }
        static async Task<WinFormsApp> InitApp(string[] args)
        {
            var builder = WinFormsAppBuilder.CreateDefault(args);
            builder.Services.AddBlazorJSRuntime();
            // Dokan Service
            builder.Services.AddSingleton<WebFSServer>();
            builder.Services.AddSingleton<DokanService>();

            // Start
            var host = builder.Build();
            return host;
        }
    }
}