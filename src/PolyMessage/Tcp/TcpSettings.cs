using System;
using System.Threading;

namespace PolyMessage.Tcp
{
    /// <summary>
    /// The settings specific to the <see cref="TcpTransport"/>.
    /// </summary>
    public sealed class TcpSettings
    {
        /// <summary>
        /// The infinite timeout as defined in <see cref="Timeout.InfiniteTimeSpan"/> field.
        /// </summary>
        public static readonly TimeSpan InfiniteTimeout = Timeout.InfiniteTimeSpan;

        public TcpSettings()
        {
            NoDelay = true;
            ServerSideClientIdleTimeout = InfiniteTimeout;
        }

        /// <summary>
        /// The TCP setting for using Nagle's algorithm. The default value is true.
        /// </summary>
        public bool NoDelay { get; set; }

        /// <summary>
        /// The interval during which a client is allowed to be idle before being disconnected from the server.
        /// The default value is <see cref="InfiniteTimeout"/>.
        /// </summary>
        public TimeSpan ServerSideClientIdleTimeout { get; set; }
    }
}
