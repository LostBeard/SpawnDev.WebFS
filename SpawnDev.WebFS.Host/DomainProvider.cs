using Dapper.Contrib.Extensions;

namespace SpawnDev.WebFS.Host
{
    public class DomainProvider
    {
        /// <summary>
        /// The provider host
        /// </summary>
        [ExplicitKey]
        public string Host { get; set; }
        /// <summary>
        /// If set to true, this provider is allowed
        /// </summary>
        public bool? Enabled { get; set; }
        /// <summary>
        /// When first seen
        /// </summary>
        public DateTimeOffset FirstSeen { get; set; }
        /// <summary>
        /// When last seen
        /// </summary>
        public DateTimeOffset LastSeen { get; set; }
        /// <summary>
        /// Url to the domain's root Url
        /// </summary>
        public string Url { get; set; }
    }
}
