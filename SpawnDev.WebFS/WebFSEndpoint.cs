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
        /// <summary>
        /// Endpoint url
        /// </summary>
        public string Url => $"ws://127.0.0.1:{Port}/{Path}";
        /// <summary>
        /// Last checked
        /// </summary>
        public DateTime LastChecked { get; set; } = DateTime.MinValue;
        /// <summary>
        /// Last verified
        /// </summary>
        public DateTime LastVerified { get; set; } = DateTime.MinValue;
    }
}
