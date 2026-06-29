using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.WebFS.MessagePack;
using System.Collections;

namespace SpawnDev.WebFS
{
    public enum ArgPackType
    {
        /// <summary>
        /// The arg is contained in the arg list
        /// </summary>
        Inline,
        /// <summary>
        /// The arg will be a follow up send
        /// </summary>
        FollowUp,
        /// <summary>
        /// The arg will not be packed. It is already binary.
        /// </summary>
        Binary,
        /// <summary>
        /// The arg will be packed using MessagePack
        /// </summary>
        MessagePack,
    }
    public enum WebSocketReadyState
    {
        Connecting,
        Open,
        Closing,
        Closed,
    }
    public class WebSocketConnectionJS : WebFSDispatcher, IDisposable, IWebSocketConnection
    {
        public static async Task<WebSocketConnectionJS?> ConnectAsync(IServiceProvider serviceProvider, string url, CancellationToken? cancellationToken = null)
        {
            var tcs = new TaskCompletionSource();
            var ct = cancellationToken ?? CancellationToken.None;
            if (cancellationToken?.IsCancellationRequested == true) throw new TaskCanceledException();
            var webSocket = new WebSocket(url);
            webSocket.OnOpen += tcs.SetResult;
            webSocket.OnError += tcs.SetResult;
            if (ct != CancellationToken.None)
                await tcs.Task.WaitAsync(ct);
            else
                await tcs.Task;
            webSocket.OnOpen -= tcs.SetResult;
            webSocket.OnError -= tcs.SetResult;
            if (ct.IsCancellationRequested) return null;
            if (webSocket.ReadyState != 1)
            {
                webSocket.Dispose();
                return null;
            }
            return new WebSocketConnectionJS(serviceProvider, webSocket, url);
        }
        public string ConnectionId { get; } = Guid.NewGuid().ToString();
        public WebSocket? WebSocket { get; private set; }
        public int BufferSize { get; set; } = 128 * 1024; // 8192;
        public object Tag { get; set; } = null;
        public Dictionary<string, string> RequestHeaders = new Dictionary<string, string>();
        public string RemoteAddress { get; protected set; } = "";
        public string UserAgent { get; protected set; } = "";
        public Uri RequestOrigin { get; protected set; }
        public Uri RequestUri { get; protected set; }
        public bool IsConnecting => (WebSocket?.ReadyState ?? 3) == 0;
        public bool IsConnected => (WebSocket?.ReadyState ?? 3) == 1;
        public bool IsDisconnecting => (WebSocket?.ReadyState ?? 3) == 2;
        public bool IsClosed => (WebSocket?.ReadyState ?? 3) == 3;
        public WebSocketReadyState State => (WebSocketReadyState)(WebSocket?.ReadyState ?? 3);
        public DateTime WhenConnected { get; private set; } = DateTime.UtcNow;
        public WebSocketConnectionJS(IServiceProvider serviceProvider, WebSocket webSocket, string remoteAddress, string? connectionId = null, bool startDataListener = true) : base(serviceProvider)
        {
            WebSocket = webSocket;
            if (!string.IsNullOrEmpty(connectionId)) ConnectionId = connectionId;
            RemoteAddress = remoteAddress;
            WebSocket.OnClose += WebSocket_OnClose;
            WebSocket.OnError += WebSocket_OnError;
            WebSocket.OnMessage += WebSocket_OnMessage;
            WebSocket.OnOpen += WebSocket_OnOpen;
            base.SendReadyFlag();
        }
        void WebSocket_OnOpen(Event e)
        {
            //BlazorJSRuntime.JS.Log("WebSocket_OnOpen", e);
            StateHasChange();
        }
        void WebSocket_OnClose(CloseEvent e)
        {
            //BlazorJSRuntime.JS.Log("WebSocket_OnClose", e);
            StateHasChange();
        }
        void WebSocket_OnError(Event e)
        {
            //BlazorJSRuntime.JS.Log("WebSocket_OnError", e);
            StateHasChange();
        }
        async void WebSocket_OnMessage(MessageEvent e)
        {
            var dataType = e.TypeOfData;
            //BlazorJSRuntime.JS.Log("WebSocket_OnMessage", dataType, e);
            try
            {
                switch (dataType)
                {
                    case "String":

                        break;
                    case "ArrayBuffer":
                        {
                            using var dataArrayBuffer = e.GetData<ArrayBuffer>();
                            await DecodeMessage(dataArrayBuffer);
                        }
                        break;
                    case "Blob":
                        {
                            using var dataBlob = e.GetData<Blob>();
                            using var dataArrayBuffer = await dataBlob.ArrayBuffer();
                            await DecodeMessage(dataArrayBuffer);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                BlazorJSRuntime.JS.Log("WebSocket_OnMessage exception" + ex.ToString());
            }
        }
        IMessagePackElement?[]? _args = null;
        ArgPackType[]? _argTypes = null;
        async Task DecodeMessage(ArrayBuffer dataArrayBuffer)
        {
            try
            {
                var dataUint8Array = new Uint8Array(dataArrayBuffer);
                if (ChunkedSender)
                {
                    // instaed of gettign a full lsit of args in 1 message, it is broken up into multiple messages
                    // 1 - arg map. Example: [ 0 ]
                    // 2 - array/list of non-binary args
                    // 3+ - 
                    if (_argTypes == null || _args == null)
                    {
                        var mlist = MessagePackElementJS.Deserialize<MessagePackListJS>(dataUint8Array);
                        _argTypes = mlist.Shift<ArgPackType[]>();
                        _args = new IMessagePackElement?[_argTypes.Length];
                        for(var i = 0; i < _argTypes.Length; i++)
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
                                case ArgPackType.FollowUp:
                                    {
                                        if (_args![i] == null)
                                        {
                                            // this is the next variable that needs a followup
                                            _args![i] = new MessagePackElementUnpacked(dataUint8Array);
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
                    var args = MessagePackElementJS.Deserialize<MessagePackListJS>(dataUint8Array);
                    //BlazorJSRuntime.JS.Log("DecodeMessage args", args);
                    await HandleCall(args);
                }
            }
            catch (Exception ex)
            {
                BlazorJSRuntime.JS.Log("WebSocket_OnMessage exception" + ex.ToString());
            }
        }
        public event Action<IWebSocketConnection> OnStateChanged = default!;

        void StateHasChange()
        {
            OnStateChanged?.Invoke(this);
        }
        public async Task Disconnect()
        {
            if (IsDisconnecting || !IsConnected || WebSocket == null) return;
            try
            {
                WebSocket.Close();
            }
            catch { }
            StateHasChange();
        }
        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed) return;
            //BlazorJSRuntime.JS.Log($"{this.GetType().Name}.Dispose()");
            if (WebSocket != null)
            {
                WebSocket.OnClose -= WebSocket_OnClose;
                WebSocket.OnError -= WebSocket_OnError;
                WebSocket.OnMessage -= WebSocket_OnMessage;
                WebSocket.OnOpen -= WebSocket_OnOpen;
                try
                {
                    WebSocket.Close();
                }
                catch { }
                WebSocket.Dispose();
                WebSocket = null;
            }
            base.Dispose();
            StateHasChange();
        }
        public static bool ChunkedSender { get; } = false;

        /// <inheritdoc/>
        protected override void SendCall(object?[] args)
        {
            Send(args);
        }
        bool Send(object?[] data)
        {
            try
            {
                SendStream(data);
                return true;
            }
            catch { }
            return false;
        }
        void SendStream(object?[] data)
        {
            if (State != WebSocketReadyState.Open)
            {
                throw new Exception("WebSocket not connected");
            }
            try
            {
                //BlazorJSRuntime.JS.Log("Encode", data);
                if (ChunkedSender)
                {
                    var followUps = new List<object>();
                    var metaData = new ArgPackType[data.Length];
                    for (var i = 0; i < data.Length; i++)
                    {
                        var b = data[i];
                        if (b is Uint8Array)
                        {
                            metaData[i] = ArgPackType.FollowUp;
                            followUps.Add(b);
                            data[i] = null;
                        }
                        else if (b is byte[])
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
                        using var packedMsg = MessagePackElementJS.Serialize(data);
                        WebSocket!.Send(packedMsg);
                    }
                    foreach(var b in followUps)
                    {
                        if (b is Uint8Array uint8Array)
                        {
                            WebSocket!.Send(uint8Array);
                        }
                        else if (b is byte[] byteArray)
                        {
                            WebSocket!.Send(byteArray);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                else
                {
                    using var packedMsg = MessagePackElementJS.Serialize(data);
                    WebSocket!.Send(packedMsg);
                }
            }
            catch (Exception ex)
            {
                var bb = "";
                throw;
            }
        }
    }
}