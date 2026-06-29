using SpawnDev.WebFS.MessagePack;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.Versioning;

namespace SpawnDev.WebFS
{

    public class WebSocketConnection : WebFSDispatcher, IDisposable, IWebSocketConnection
    {
        public static async Task<WebSocketConnection?> ConnectAsync(IServiceProvider serviceProvider, string url, CancellationToken? cancellationToken = null)
        {
            using var cts = new CancellationTokenSource(5000);
            var ct = cancellationToken ?? cts.Token;
            if (ct.IsCancellationRequested == true) throw new TaskCanceledException();
            var webSocket = new ClientWebSocket();
            try
            {
                await webSocket.ConnectAsync(new Uri(url), ct);
            }
            catch { }
            var connected = webSocket.State == WebSocketState.Open;
            if (!connected)
            {
                webSocket.Dispose();
                return null;
            }
            return new WebSocketConnection(serviceProvider, webSocket, url);
        }
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
        IMessagePackElement?[]? _args = null;
        ArgPackType[]? _argTypes = null;
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
            DataListenerTask = Task.Run((Func<Task?>)(async () =>
            {
                base.SendReadyFlag();
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
                                _ = Task.Run((Func<Task?>)(async () =>
                                {
                                    if (WebSocketConnectionJS.ChunkedSender)
                                    {
                                        // instaed of gettign a full lsit of args in 1 message, it is broken up into multiple messages
                                        // 1 - arg map. Example: [ 0 ]
                                        // 2 - array/list of non-binary args
                                        // 3+ - 
                                        if (_argTypes == null || _args == null)
                                        {
                                            var mlist = MessagePackElement.DeserializeList(ms.ToArray());
                                            _argTypes = mlist.Shift<ArgPackType[]>();
                                            _args = new IMessagePackElement?[_argTypes.Length];
                                            for (var i = 0; i < _argTypes.Length; i++)
                                            {
                                                var argType = _argTypes[i];
                                                switch (argType)
                                                {
                                                    case ArgPackType.Inline:
                                                        _args[i] = mlist.GetElement(i) ?? new MessagePackElementUnpacked(null);
                                                        break;
                                                    case ArgPackType.FollowUp:
                                                        // this will arrive separete from the arg list
                                                        break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            for (var i = 0; i < _argTypes.Length; i++)
                                            {
                                                var argType = _argTypes[i];
                                                switch (argType)
                                                {
                                                    case ArgPackType.Inline:

                                                        break;
                                                    case ArgPackType.FollowUp:
                                                        {
                                                            if (_args![i] == null)
                                                            {
                                                                // this is the next variable that needs a followup
                                                                _args![i] = new MessagePackElementUnpacked(ms.ToArray());
                                                                break;
                                                            }
                                                        }
                                                        break;
                                                }
                                            }
                                        }
                                        // if all followups have been received we can now process the full call
                                        var done = !_args!.Any(o => o == null);
                                        if (done)
                                        {
                                            var argCollection = new MessagePackCollection(_args.Select(o => (IMessagePackElement)o!).ToList());
                                            _argTypes = null;
                                            _args = null;
                                            await HandleCall(argCollection);
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            var args = MessagePackElement.DeserializeList(ms.ToArray());
                                            if (args.Count > 0)
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
                                    }
                                }));
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
            }));
            return true;
        }

        public event Action<IWebSocketConnection> OnStateChanged = default!;

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
            WebSocket?.Dispose();
            base.Dispose();
            StateHasChange();
        }

        // IMPORTANT
        // the semaphore limiter is required because (as the exception states):
        // while readAsync and sendAsync can be used simultaneously, only 1 outstanding call of each is allowed at a time
        // the semaphore prevents this error
        SemaphoreSlim sendAsyncLimiter = new SemaphoreSlim(1);
        public int SendTimeout = 10000;
        //async Task<bool> Send(object?[] data)
        //{
        //    try
        //    {
        //        var jsonS = MessagePackElement.Serialize(data);
        //        await SendBytes(jsonS);
        //        return true;
        //    }
        //    catch { }
        //    return false;
        //}
        async Task<bool> SendAsync(object?[] data)
        {
            try
            {
                await SendStream(data);
                return true;
            }
            catch { }
            return false;
        }
        async Task SendStream(object?[] data)
        {
            if (WebSocket == null || WebSocket.State != WebSocketState.Open)
            {
                throw new Exception("WebSocket not connected");
            }
            try
            {
                await sendAsyncLimiter.WaitAsync().ConfigureAwait(false);

                if (WebSocketConnectionJS.ChunkedSender)
                {
                    var followUps = new List<object>();
                    var metaData = new ArgPackType[data.Length];
                    for (var i = 0; i < data.Length; i++)
                    {
                        var b = data[i];
                        if (b is byte[])
                        {
                            metaData[i] = ArgPackType.FollowUp;
                            followUps.Add(b);
                            data[i] = null;
                        }
                        else
                        {
                            metaData[i] = ArgPackType.Inline;
                        }
                    }
                    // prepend the metadata to the arg list
                    data = new object?[] { metaData }.Concat(data).ToArray();
                    {
                        // send the manifest and the arglist
                        var writableWebSocketStream = new WritableWebSocketStream(WebSocket);
                        await MessagePackElement.SerializeAsync(writableWebSocketStream, data).ConfigureAwait(false);
                        await writableWebSocketStream.EndMessage();
                    }
                    foreach (var b in followUps)
                    {
                        if (b is byte[] byteArray)
                        {
                            await WebSocket.SendAsync(byteArray, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
                        }
                        else
                        {
                            throw new NotImplementedException("Should not be a followup");
                        }
                    }
                }
                else
                {
                    var writableWebSocketStream = new WritableWebSocketStream(WebSocket);
                    await MessagePackElement.SerializeAsync(writableWebSocketStream, data).ConfigureAwait(false);
                    await writableWebSocketStream.EndMessage();
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
        /// <inheritdoc/>
        protected override void SendCall(object?[] args)
        {
            _ = SendAsync(args);
        }

    }
}

