namespace SpawnDev.WebFS
{
    public class WebFSEndpoint
    {
        public EndpointResult Result { get; set; }
        public ushort Port { get; set; }
        public string Path { get; set; } = "";
        public string Url => $"ws://127.0.0.1:{Port}/{Path}";
        public DateTime LastChecked { get; set; } = DateTime.MinValue;
        public DateTime LastVerified { get; set; } = DateTime.MinValue;
    }
}
