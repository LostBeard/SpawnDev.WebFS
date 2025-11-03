using System.Net;

namespace SpawnDev.WebFS.Host
{
    public class ConnectionRequestArgs
    {
        public string ConnectionId { get; set; }
        public HttpListenerContext Context { get; set; }
        public bool CancelConnection { get; set; } = false;
    }
}
