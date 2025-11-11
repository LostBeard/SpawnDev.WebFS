using Microsoft.AspNetCore.Components;
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
    public class WebFSClient : IDisposable
    {
        /// <summary>
        /// The tray app base port
        /// </summary>
        public ushort BasePort { get; private set; } = 6565;
        /// <summary>
        /// The number of ports to try
        /// </summary>
        public int MaxPortSpread { get; } = 4;
        /// <summary>
        /// Tray app endpoints to try
        /// </summary>
        public List<WebFSEndpoint> Endpoints { get; } = new List<WebFSEndpoint>();
        /// <summary>
        /// Current tray app endpoint
        /// </summary>
        public WebFSEndpoint Endpoint { get; private set; }
        IServiceProvider ServiceProvider;
        /// <summary>
        /// The current tray app dispatcher
        /// </summary>
        public WebSocketConnection? Tray { get; private set; }
        /// <summary>
        /// Returns true if connected to the tray app
        /// </summary>
        public bool Connected { get; private set; }
        /// <summary>
        /// The host name serving this app
        /// </summary>
        public string Host { get; }
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="navigationManager"></param>
        public WebFSClient(IServiceProvider serviceProvider, NavigationManager navigationManager)
        {
            ServiceProvider = serviceProvider;
            Host = new Uri(navigationManager.BaseUri).Host;
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
        bool _Enabled = false;
        /// <summary>
        /// Returns true if auto-connect is enabled.<br/>
        /// If connected and set to false, the connection will be closed.<br/>
        /// </summary>
        public bool Enabled
        {
            get => _Enabled;
            set
            {
                if (_Enabled == value) return;
                if (!value)
                {
                    _ = Disconnect();
                }
                else
                {
                    _ = Connect();
                }
            }
        }
        /// <summary>
        /// Disconnect if connected and disable auto-connect
        /// </summary>
        /// <returns></returns>
        public async Task Disconnect()
        {
            if (!_Enabled) return;
            _Enabled = false;
            _disconnectRequested?.Cancel();
            _disconnectRequested?.Dispose();
            _disconnectRequested = null;
            if (WebSocket != null)
            {
                try
                {
                    await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", CancellationToken.None);
                }
                catch { }
            }
        }
        WebSocket? WebSocket = null;
        Task? _ConnectTask = null;
        /// <summary>
        /// Connect and enable auto-connect
        /// </summary>
        /// <returns></returns>
        public async Task Connect()
        {
            if (_Enabled) return;
            _Enabled = true;
            if (_ConnectTask?.IsCompleted == false)
            {
                // wait for previous run to finish up
                await _ConnectTask;
            }
            // start auto-connect task
            _disconnectRequested = new CancellationTokenSource();
            _ConnectTask = _Connect(_disconnectRequested.Token);

        }
        CancellationTokenSource? _disconnectRequested = null;
        async Task _Connect(CancellationToken token)
        {
            var endpointsTriedCount = 0;
            var endpointsCount = Endpoints.Count;
            while (!token.IsCancellationRequested)
            {

                var endPoint = GetConnectEndpoint();
                endPoint.LastChecked = DateTime.UtcNow;
                endPoint.Result = EndpointResult.Unknown;
                Endpoint = endPoint;
                using var webSocket = new ClientWebSocket();
                WebSocket = webSocket;
                try
                {
                    using var connectTCS = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await webSocket.ConnectAsync(new Uri(endPoint.Url), connectTCS.Token);
                }
                catch { }
                var connected = webSocket.State == WebSocketState.Open;
                if (connected)
                {
                    var _disconnectTCS = new TaskCompletionSource();
                    Tray = new WebSocketConnection(ServiceProvider, webSocket, endPoint.Url);
                    Tray.OnStateChanged += (_) =>
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
                        await Tray.WhenReady.WaitAsync(TimeSpan.FromSeconds(5));
                    }
                    catch { }
                    if (Tray.Ready)
                    {
                        endPoint.Result = EndpointResult.Verified;
                        endPoint.LastChecked = DateTime.UtcNow;
                        endPoint.LastVerified = DateTime.UtcNow;
                        Connected = true;
                        OnConnected?.Invoke(this);
                        // wait for disconnect
                        await _disconnectTCS.Task;
                        Connected = false;
                        OnDisconnected?.Invoke(this);
                        endPoint.LastChecked = DateTime.UtcNow;
                        endPoint.LastVerified = DateTime.UtcNow;
                    }
                    Tray.Dispose();
                    Tray = null;
                }
                WebSocket = null;
                if (endPoint.Result == EndpointResult.Unknown)
                {
                    endPoint.Result = EndpointResult.Invalid;
                    endpointsTriedCount++;
                    if (endpointsTriedCount == endpointsCount)
                    {
                        endpointsTriedCount = 0;
                        if (token.IsCancellationRequested) break;
                        await Task.Delay(5000);
                    }
                    else
                    {
                        if (token.IsCancellationRequested) break;
                        await Task.Delay(1000);
                    }
                }
                else
                {
                    if (token.IsCancellationRequested) break;
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
            _ = Disconnect();
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
