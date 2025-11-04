using DokanNet;
using DokanNet.Logging;
using SpawnDev.BlazorJS;
using SpawnDev.WebFS.DokanAsync;

namespace SpawnDev.WebFS.Host
{
    public class DokanService : IAsyncBackgroundService, IDisposable
    {
        public Task Ready => _Ready ??= InitAsync();
        Task? _Ready = null;
        WebFSServer WebFSServer;
        public string MountPoint { get; private set; }
        ConsoleLogger? dokanLogger;
        Dokan? dokan;
        DokanInstance? dokanInstance;
        public DokanService(WebFSServer webFSServer)
        {
            WebFSServer = webFSServer;
            MountPoint = @"q:\";
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
            }
            catch (DokanException ex)
            {
                Console.WriteLine(@"Error: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Verify Dokan 2.3.1.1000 or later is installed and try again. Error: " + ex.Message);
            }
        }
        public void Dispose()
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
    }
}
