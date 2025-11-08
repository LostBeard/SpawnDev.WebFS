using MessagePack;
using SpawnDev.WebFS.MessagePack;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.Versioning;
using System.Text;

namespace SpawnDev.WebFS
{
    public class WebSocketConnection : WebFSDispatcher, IDisposable
    {
        public string ConnectionId { get; } = Guid.NewGuid().ToString();
        public WebSocket? WebSocket { get; private set; }
        public int BufferSize { get; set; } = 128 * 1024; // 8192;
        CancellationTokenSource _cancellationTokenSourceLocal = null;
        public object Tag { get; set; } = null;
        public Dictionary<string, string> RequestHeaders = new Dictionary<string, string>();
        public string RemoteAddress { get; protected set; } = "";
        public string UserAgent { get; protected set; } = "";
        public Uri RequestOrigin { get; protected set; }
        public Uri RequestUri { get; protected set; }
        private Task DataListenerTask = null;
        public bool IsConnected => WebSocket?.State == WebSocketState.Open;
        public bool IsClosed => State == WebSocketState.Closed;
        public WebSocketState State => WebSocket?.State ?? WebSocketState.Closed;
        public bool IsDisconnecting => WebSocket?.State == WebSocketState.CloseReceived || WebSocket?.State == WebSocketState.CloseSent;
        public HttpListenerContext Context { get; }
        public DateTime WhenConnected { get; private set; } = DateTime.UtcNow;
        [SupportedOSPlatform("windows")]
        public WebSocketConnection(IServiceProvider serviceProvider, WebSocket webSocket, HttpListenerContext context, string? connectionId = null, bool startDataListener = true) : base(serviceProvider)
        {
            WebSocket = webSocket;
            Context = context;
            if (!string.IsNullOrEmpty(connectionId)) ConnectionId = connectionId;
            RemoteAddress = context.Request.RemoteEndPoint.Address.ToString();
            RequestUri = context.Request.Url!;
            UserAgent = context.Request.UserAgent;
            var origin = context.Request.Headers.GetValues("origin")?.FirstOrDefault();
            if (string.IsNullOrEmpty(origin))
            {
                throw new Exception("Invalid origin");
            }
            try
            {
                RequestOrigin = new Uri(origin);
            }
            catch
            {
                throw new Exception("Invalid origin");
            }
            if (IsConnected && startDataListener) Listen();
        }
        public WebSocketConnection(IServiceProvider serviceProvider, WebSocket webSocket, string remoteAddress, string? connectionId = null, bool startDataListener = true) : base(serviceProvider)
        {
            WebSocket = webSocket;
            if (!string.IsNullOrEmpty(connectionId)) ConnectionId = connectionId;
            RemoteAddress = remoteAddress;
            if (IsConnected && startDataListener) Listen();
        }
        public bool Listen(WebSocket? webSocket = null)
        {
            if (webSocket != null && WebSocket != webSocket)
            {
                if (WebSocket?.State == WebSocketState.Open)
                {
                    // already listening
                    return false;
                }
                WebSocket = webSocket;
            }
            if (WebSocket?.State != WebSocketState.Open)
            {
                return false;
            }
            webSocket = WebSocket;
            var cancellationTokenSourceLocal = new CancellationTokenSource();
            _cancellationTokenSourceLocal = cancellationTokenSourceLocal;
            DataListenerTask = Task.Run(async () =>
            {
                SendReadyFlag();
                if (webSocket!.State == WebSocketState.Open)
                {
                    var buffer = new ArraySegment<byte>(new byte[BufferSize]);
                    while (!cancellationTokenSourceLocal.IsCancellationRequested && webSocket.State == WebSocketState.Open)
                    {
                        WebSocketReceiveResult? result = null;
                        try
                        {
                            var ms = new MemoryStream();
                            do
                            {
                                result = await webSocket.ReceiveAsync(buffer, cancellationTokenSourceLocal.Token);
                                ms.Write(buffer.Array!, buffer.Offset, result.Count);
                            } while (!result.EndOfMessage);
                            ms.Seek(0, SeekOrigin.Begin);
                            if (ms.Length > 0)
                            {
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        var args = MessagePackSerializer.Deserialize<MessagePackList>(ms);
                                        if (args.Length > 0)
                                        {
                                            await HandleCall(args);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        var nmt = ex.ToString();
                                    }
                                    finally
                                    {
                                        ms.Dispose();
                                    }
                                });
                            }
                        }
                        catch (WebSocketException ex)
                        {
                            // likely just a disconnect
                            // can be ignored
                            var bb = "";
                        }
                        catch (Exception ex)
                        {
                            var t = ex.GetType();
#if DEBUG
                            Console.WriteLine($"RTLinkShared.Tray.ProcessMessage() IsClient: {ConnectionId} Exception: {ex.Message}");
#endif
                        }
                        if (result != null && result.MessageType == WebSocketMessageType.Close) break;
                    }
                }
                WebSocket = null;
                webSocket.Dispose();
                cancellationTokenSourceLocal.Dispose();
                StateHasChange();
            });
            return true;
        }

        public event Action<WebSocketConnection> OnStateChanged = default!;

        void StateHasChange()
        {
            OnStateChanged?.Invoke(this);
        }

        public async Task Disconnect()
        {
            if (IsDisconnecting || !IsConnected || WebSocket == null) return;
            if (_cancellationTokenSourceLocal == null) return;
            try
            {
                await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
                _cancellationTokenSourceLocal?.Cancel();
            }
            catch { }
            _cancellationTokenSourceLocal = null;
            WebSocket = null;
            StateHasChange();
        }
        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed) return;
            Console.WriteLine($"{this.GetType().Name}.Dispose()");
            //Disconnect().Wait();
            WebSocket?.Dispose();
            base.Dispose();
            StateHasChange();
        }

        // IMPORTANT
        // the semaphore limiter is required because (as the exception states):
        // while readAsync and sendAsync be used simultaneously, only 1 outstanding call of each is allowed at a time
        // the semaphore prevents this error
        SemaphoreSlim sendAsyncLimiter = new SemaphoreSlim(1);
        public int SendTimeout = 10000;
        public async Task<bool> Send(object?[] data)
        {
            try
            {
                var jsonS = MessagePackSerializer.Serialize(data);
                await PipeOutRawBytes(jsonS);
                return true;
            }
            catch { }
            return false;
        }
        async Task PipeOutRawBytes(byte[] data)
        {
            if (WebSocket == null || WebSocket.State != WebSocketState.Open)
            {
                throw new Exception("Websocket not connected");
            }
            try
            {
                await sendAsyncLimiter.WaitAsync();
                var buffer = new ArraySegment<byte>(data);
                using (var s_cts = new CancellationTokenSource())
                {
                    s_cts.CancelAfter(SendTimeout);
                    await WebSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, s_cts.Token).ConfigureAwait(false);
                }
            }
            catch (WebSocketException ex)
            {
                // likely just a disconnect
                // can be ignored
                var bb = "";
                throw;
            }
            catch (Exception ex)
            {
                var bb = "";
                throw;
            }
            finally
            {
                sendAsyncLimiter.Release();
            }
        }
        async Task PipeOutRawString(string data)
        {
            if (WebSocket == null || WebSocket.State != WebSocketState.Open)
            {
                throw new Exception("Websocket not connected");
            }
            var releaseLimiter = true;
            try
            {
                await sendAsyncLimiter.WaitAsync();
                var msg = Encoding.UTF8.GetBytes(data);
                var buffer = new ArraySegment<byte>(msg, 0, msg.Length);
                using (var s_cts = new CancellationTokenSource())
                {
                    s_cts.CancelAfter(SendTimeout);
                    await WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, s_cts.Token).ConfigureAwait(false);
                }
            }
            catch (WebSocketException ex)
            {
                // likely just a disconnect
                // can be ignored
                var bb = "";
                throw;
            }
            catch (Exception ex)
            {
                var bb = "";
                throw;
            }
            finally
            {
                if (releaseLimiter) sendAsyncLimiter.Release();
            }
        }
        protected override void SendCall(object?[] args)
        {
            _ = Send(args);
        }
        //protected override Task<object?> DispatchCallAsync(MethodInfo methodInfo, object?[]? args)
        //{
        //    return base.DispatchCallAsync(methodInfo, args);
        //}
    }
}

