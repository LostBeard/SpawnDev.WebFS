namespace SpawnDev.WebFS
{
    public interface IWebSocketConnection
    {
        int BufferSize { get; set; }
        string ConnectionId { get; }
        //HttpListenerContext Context { get; }
        bool IsClosed { get; }
        bool IsConnected { get; }
        bool IsDisconnecting { get; }
        string RemoteAddress { get; }
        Uri RequestOrigin { get; }
        Uri RequestUri { get; }
        //WebSocketState State { get; }
        object Tag { get; set; }
        string UserAgent { get; }
        //WebSocket? WebSocket { get; }
        DateTime WhenConnected { get; }
        event Action<IWebSocketConnection> OnStateChanged;
        Task WhenReady { get; }
        bool Ready { get; }
        Task Disconnect();
        void Dispose();
        //bool Listen(WebSocket? webSocket = null);
    }
}

