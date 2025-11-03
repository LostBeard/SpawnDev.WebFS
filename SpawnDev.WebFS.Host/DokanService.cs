using DokanNet;
using DokanNet.Logging;
using SpawnDev.BlazorJS;
using SpawnDev.WebFS.DokanAsync;

namespace SpawnDev.WebFS.Host
{
    public class DokanService : IAsyncBackgroundService
    {
        public Task Ready => _Ready ??= InitAsync();
        Task? _Ready = null;
        WebFSServer? rfs = null;
        IServiceProvider ServiceProvider;
        public DokanService(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
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
                using (var mre = new ManualResetEvent(false))
                using (var dokanLogger = new ConsoleLogger("[Dokan] "))
                using (var dokan = new Dokan(dokanLogger))
                {
                    Console.CancelKeyPress += (sender, e) =>
                    {
                        e.Cancel = true;
                        mre.Set();
                    };
                    rfs = new WebFSServer(ServiceProvider);
                    var dokanBuilder = new DokanInstanceBuilder(dokan)
                        .ConfigureOptions(options =>
                        {
                            options.Options = DokanOptions.StderrOutput;
                            options.MountPoint = "q:\\";
                        });
                    using (var dokanInstance = dokanBuilder.Build(rfs))
                    {
                        mre.WaitOne();
                    }
                    Console.WriteLine(@"Success");
                }
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
    }
}
