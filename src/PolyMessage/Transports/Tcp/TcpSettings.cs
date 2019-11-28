using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace PolyMessage.Transports.Tcp
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
            TlsProtocol = SslProtocols.None;
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

        /// <summary>
        /// The certificate used in TLS over TCP.
        /// </summary>
        public X509Certificate2 TlsServerCertificate { get; set; }

        /// <summary>
        /// The validation callback used by the client when validating the server certificate.
        /// </summary>
        public RemoteCertificateValidationCallback TlsClientRemoteCertificateValidationCallback { get; set; }

        /// <summary>
        /// The TLS protocol used in the communication.
        /// </summary>
        public SslProtocols TlsProtocol { get; set; }
    }
}
