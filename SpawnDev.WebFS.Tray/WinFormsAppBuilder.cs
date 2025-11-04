using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using System.Threading.Tasks;

namespace SpawnDev.WebFS.Tray
{
    class WinFormsAppBuilder
    {
        public string[]? Args { get; } = null;
        public IServiceCollection Services { get; }
        public WinFormsAppBuilder(string[]? args = null)
        {
            Args = args;
            HostingEnvironment env = new HostingEnvironment();
            env.ContentRootPath = Directory.GetCurrentDirectory();
#if DEBUG
            env.EnvironmentName = "Development";
#else
            env.EnvironmentName = "Production";
#endif
            ////Startup startup = new Startup(env);
            Services = new ServiceCollection();
            Services.AddSingleton(env);
            Services.AddSingleton(Services);
            Services.AddSingleton<IServiceProvider>(sp => sp);
        }
        public static WinFormsAppBuilder CreateDefault(string[]? args = null)
        {
            var builder = new WinFormsAppBuilder(args);
            return builder;
        }
        public WinFormsApp Build()
        {
            var serviceProvider = Services.BuildServiceProvider();
            return new WinFormsApp(Args, serviceProvider);
        }
    }
    public class WinFormsApp : IDisposable
    {
        public string[]? Args { get; } = null;
        public IServiceProvider Services { get; }
        public WinFormsApp(string[]? args, IServiceProvider services)
        {
            Args = args;
            Services = services;
        }
        public bool IsDisposed { get; set; }
        public bool IsDisposing { get; set; }
        public void Dispose()
        {
            if (IsDisposed || IsDisposing) return;
            IsDisposing = true;
            // dispose
            if (Services is ServiceProvider disposable) disposable.Dispose();
            IsDisposed = true;
            IsDisposing = false;
        }
    }
}
