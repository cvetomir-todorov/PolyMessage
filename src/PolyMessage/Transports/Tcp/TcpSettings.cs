using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace PolyMessage.Transports.Tcp
{
    /// <summary>
    /// The settings specific to the <see cref="TcpTransport"/>.
    /// </summary>
    public sealed class TcpSettings
    {
        /// <summary>
        /// The TCP setting for using Nagle's algorithm. The default value is true.
        /// </summary>
        public bool NoDelay { get; set; } = true;

        /// <summary>
        /// The size of the TCP send buffer in bytes.
        /// </summary>
        public int SendBufferSize { get; set; } = 8192; // 8KB

        /// <summary>
        /// The size of the TCP receive buffer in bytes.
        /// </summary>
        public int ReceiveBufferSize { get; set; } = 8192; // 8KB

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
        public SslProtocols TlsProtocol { get; set; } = SslProtocols.None;
    }
}
