using SpawnDev.BlazorJS;
using System.Net.WebSockets;


namespace SpawnDev.WebFS
{
    /// <summary>
    /// Connects to the WebFSHServer app running on the same computer as the browser running the site running this code.<br/>
    /// When connected to WebFSHServer, this client can register a mount point that it will then handle all requests for.<br/>
    /// This allows websites the ability to host a folder on the users computer that can be accessed by normal apps like any other folders.<br/>
    /// The user will have control over what sites and when they can access this feature, including blocking certain file types from being hosted, 
    /// such as .exe, .dll, .bat, .sys, .cmd, etc... which may be harmful if abused.
    /// </summary>
    public class WebFSClient : IDisposable, IAsyncBackgroundService
    {
        Task? _Ready;
        /// <inheritdoc/>
        public Task Ready => _Ready ??= InitAsync();
        BlazorJSRuntime JS;
        public string Url { get; private set; }
        public ushort BasePort { get; private set; } = 6565;
        public int MaxPortSpread { get; } = 4;
        public List<WebFSEndpoint> Endpoints { get; } = new List<WebFSEndpoint>();
        public WebFSEndpoint Endpoint { get; private set; }
        IServiceProvider ServiceProvider;
        public WebSocketConnection? Connection { get; private set; }
        TaskCompletionSource? _disconnectTCS;
        public bool Connected { get; private set; }
        public WebFSProvider WebFSProvider { get; private set; }
		public WebFSClient(BlazorJSRuntime js, WebFSProvider webFSProvider, IServiceProvider serviceProvider)
        {
			WebFSProvider = webFSProvider;
			ServiceProvider = serviceProvider;
            JS = js;
            for (var i = 0; i < MaxPortSpread; i++)
            {
                var port = BasePort + i;
                if (port >= ushort.MaxValue) break;
                Endpoints.Add(new WebFSEndpoint
                {
                    Port = (ushort)port,
                    Path = "",
                    LastChecked = DateTime.MinValue + TimeSpan.FromSeconds(i),
                });
            }
            Endpoint = Endpoints.First();
        }
        async Task InitAsync()
        {
            await WebFSProvider.Ready;

			_ = Task.Run(() => Connect());
        }
        WebFSEndpoint GetConnectEndpoint()
        {
            var p = Endpoints.OrderByDescending(o => o.LastChecked).First();
            if (p.Result == EndpointResult.Verified)
            {
                return p;
            }
            p = Endpoints.OrderBy(o => o.LastChecked).First();
            return p;
        }
        async Task Connect()
        {
            var endpointsTriedCount = 0;
            var endpointsCount = Endpoints.Count;
            while (!Disposed)
            {

                var endPoint = GetConnectEndpoint();
                endPoint.LastChecked = DateTime.UtcNow;
                Endpoint = endPoint;
                endPoint.Result = EndpointResult.Unknown;
                JS.Log($"Endpoint.Url: {Endpoint.Url}");
                Url = endPoint.Url;
                using var webSocket = new ClientWebSocket();
                using var connectTCS = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try
                {
                    await webSocket.ConnectAsync(new Uri(Url), connectTCS.Token);
                }
                catch (Exception ex)
                {

                }
                var connected = webSocket.State == WebSocketState.Open;
                if (connected)
                {
                    _disconnectTCS = new TaskCompletionSource();

                    Connection = new WebSocketConnection(ServiceProvider, webSocket, Url);
                    Connection.OnStateChanged += (_) =>
                    {
                        if (webSocket.State != WebSocketState.Open)
                        {
                            _disconnectTCS?.TrySetResult();
                        }
                    };
                    endPoint.Result = EndpointResult.Verified;
                    endPoint.LastVerified = DateTime.UtcNow;
                    try
                    {
                        await Connection.WhenReady.WaitAsync(TimeSpan.FromSeconds(5));
                    }
                    catch { }
                    if (Connection.Ready)
                    {
                        endPoint.Result = EndpointResult.Verified;
                        endPoint.LastChecked = DateTime.UtcNow;
                        endPoint.LastVerified = DateTime.UtcNow;
                        Connected = true;
                        JS.Log("Connected", Url);
                        OnConnected?.Invoke(this);
                        // wait for disconnect
                        await _disconnectTCS.Task;
                        Connected = false;
                        JS.Log("Disconnected", Url);
                        OnDisconnected?.Invoke(this);
                        Connection.Dispose();
                        endPoint.LastChecked = DateTime.UtcNow;
                        endPoint.LastVerified = DateTime.UtcNow;
                    }
                }
                if (endPoint.Result == EndpointResult.Unknown)
                {
                    endPoint.Result = EndpointResult.Invalid;
                    endpointsTriedCount++;
                    if (endpointsTriedCount == endpointsCount)
                    {
                        endpointsTriedCount = 0;
                        await Task.Delay(5000);
                    }
                    else
                    {
                        await Task.Delay(1000);
                    }
                }
                else
                {
                    await Task.Delay(5000);
                }
            }
        }
        /// <summary>
        /// Fires when disconnected from the signaler
        /// </summary>
        public event Action<WebFSClient> OnDisconnected = default!;
        /// <summary>
        /// Fires when connected to the signaler
        /// </summary>
        public event Action<WebFSClient> OnConnected = default!;
        /// <summary>
        /// Called when this instance is being disposed
        /// </summary>
        /// <param name="disposing"></param>
        void Dispose(bool disposing)
        {
            if (Disposed) return;
            Disposed = true;
        }
        /// <summary>
        /// Returns true if this instance has been disposed
        /// </summary>
        public bool Disposed { get; set; }
        /// <summary>
        /// Disposes this instance
        /// </summary>
        public void Dispose()
        {
            if (Disposed) return;
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// Finalizer
        ~WebFSClient() => Dispose(false);
    }
}
