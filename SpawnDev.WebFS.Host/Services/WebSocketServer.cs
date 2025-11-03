using System.Net;
using System.Net.WebSockets;

namespace SpawnDev.WebFS.Host
{
    public class WebSocketServer
    {
        public event ConnectRequestDelegate OnConnectRequest;
        public delegate void ConnectRequestDelegate(WebSocketServer sender, ConnectionRequestArgs eventArgs);
        public event ConnectedDelegate OnConnected;
        public delegate void ConnectedDelegate(WebSocketServer sender, WebSocketConnection conn);
        public event DisconnectedDelegate OnDisconnected;
        public delegate void DisconnectedDelegate(WebSocketServer sender, WebSocketConnection conn);
        HttpListener httpListener = new HttpListener();
        CancellationTokenSource _cancellationTokenSourceLocal;
        public List<string> ListenAddresses { get; private set; } = new List<string>();
        public WebSocketServer(IServiceProvider serviceProvider, List<string> listenAddresses)
        {
            ServiceProvider = serviceProvider;
            ListenAddresses = listenAddresses;
            if (ListenAddresses == null || ListenAddresses.Count == 0)
            {
                ListenAddresses = new List<string> { "http://localhost:80/" };
            }
        }
        public WebSocketServer(IServiceProvider serviceProvider, string host = "127.0.0.1", params ushort[] ports)
        {
            ServiceProvider = serviceProvider;
            if (ports == null) ports = new ushort[] { 80 };
            foreach (var port in ports)
            {
                ListenAddresses = new List<string> { $"http://{host}:{port}/" };
            }
        }
        IServiceProvider ServiceProvider;
        public WebSocketServer(IServiceProvider serviceProvider, params ushort[] ports)
        {
            ServiceProvider = serviceProvider;
            var host = "127.0.0.1";
            if (ports == null) ports = new ushort[] { 80 };
            foreach (var port in ports)
            {
                ListenAddresses = new List<string> { $"http://{host}:{port}/" };
            }
        }

        public bool StartListening()
        {
            if (httpListener.IsListening) return true;
            try
            {
                httpListener.Prefixes.Clear();
                foreach (var address in ListenAddresses)
                {
                    httpListener.Prefixes.Add(address);
                }
                httpListener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Listen error: {ex.Message}");
                return false;
            }
            foreach (var address in ListenAddresses)
            {
                Console.WriteLine($"Listening on: {address}");
            }
            _ = Listen();
            return true;
        }

        public void StopListening()
        {
            if (!httpListener.IsListening) return;
            httpListener.Stop();
            _cancellationTokenSourceLocal?.Cancel();
            _cancellationTokenSourceLocal = null;
        }
        Dictionary<string, WebSocketConnection> _Connections = new Dictionary<string, WebSocketConnection>();
        object ConnectionsLock = new object();
        public List<WebSocketConnection> Connections
        {
            get
            {
                lock (ConnectionsLock)
                {
                    return _Connections.Values.ToList();
                }
            }
        }
        // listen for new connections
        async Task Listen()
        {
            var cancellationTokenSourceLocal = new CancellationTokenSource();
            _cancellationTokenSourceLocal = cancellationTokenSourceLocal;
            while (!cancellationTokenSourceLocal.IsCancellationRequested)
            {
                try
                {
                    HttpListenerContext context = await httpListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        var args = new ConnectionRequestArgs 
                        { 
                            Context = context,
                            ConnectionId = Guid.NewGuid().ToString(),
                        };
                        OnConnectRequest?.Invoke(this, args);
                        if (!args.CancelConnection)
                        {
                            HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                            WebSocket webSocket = webSocketContext.WebSocket;
                            if (webSocket.State == WebSocketState.Open)
                            {
                                var conn = new WebSocketConnection(ServiceProvider, webSocket, context, args.ConnectionId);
                                lock (ConnectionsLock)
                                {
                                    conn.OnStateChanged += Conn_OnStateChanged;
                                    _Connections.Add(conn.ConnectionId, conn);
                                }
                                try
                                {
                                    await conn.WhenReady.WaitAsync(TimeSpan.FromSeconds(5));
                                }
                                catch
                                {
                                    context.Response.Close();
                                    continue;
                                }
                                OnConnected?.Invoke(this, conn);
                                continue;
                            }
                        }
                    }
                    context.Response.Close();
                }
                catch (Exception ex)
                {
                    var hh = "";
                }
            }
            cancellationTokenSourceLocal.Dispose();
        }
        private void Conn_OnStateChanged(WebSocketConnection webSocket)
        {
            if (webSocket.IsClosed)
            {
                lock (ConnectionsLock)
                {
                    if (_Connections.ContainsKey(webSocket.ConnectionId))
                    {
                        webSocket.OnStateChanged -= Conn_OnStateChanged;
                        _Connections.Remove(webSocket.ConnectionId);
                    }
                }
                OnDisconnected?.Invoke(this, webSocket);
            }
        }
    }
}
