namespace SpawnDev.WebFS
{
    /// <summary>
    /// Endpoint information used when connecting to the host
    /// </summary>
    public class WebFSEndpoint
    {
        /// <summary>
        /// Last result
        /// </summary>
        public EndpointResult Result { get; set; }
        /// <summary>
        /// Port
        /// </summary>
        public ushort Port { get; set; }
        /// <summary>
        /// Path
        /// </summary>
        public string Path { get; set; } = "";
        public string Url => $"ws://127.0.0.1:{Port}/{Path}";
        public DateTime LastChecked { get; set; } = DateTime.MinValue;
        public DateTime LastVerified { get; set; } = DateTime.MinValue;
    }
}
