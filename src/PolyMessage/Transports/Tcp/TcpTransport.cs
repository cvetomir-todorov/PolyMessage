using System;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PolyMessage.Transports.Tcp
{
    public class TcpTransport : PolyTransport
    {
        private readonly ILogger _logger;

        public TcpTransport(Uri address, ILoggerFactory loggerFactory)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (!string.Equals(address.Scheme, "tcp", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Uri scheme should be TCP.");
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            Address = address;
            Settings = new TcpSettings();
            _logger = loggerFactory.CreateLogger(GetType());
        }

        public override string DisplayName => "TCP";

        public override Uri Address { get; }

        public TcpSettings Settings { get; }

        public override PolyListener CreateListener()
        {
            return new TcpListener(this, _logger);
        }

        public override PolyChannel CreateClient()
        {
            TcpClient tcpClient = new TcpClient();
            return new TcpChannel(tcpClient, this, isServer: false, _logger);
        }

        public override string GetSettingsInfo()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendFormat("NoDelay {0}", Settings.NoDelay ? "enabled" : "disabled");
            builder.AppendFormat(", TLS version {0}", Settings.TlsProtocol);
            if (Settings.TlsServerCertificate != null)
            {
                builder.AppendFormat(", TLS certificate {0}", Settings.TlsServerCertificate.Subject);
            }

            if (HostTimeouts.ClientSend != InfiniteTimeout)
            {
                builder.AppendFormat(", Host client send timeout {0}s", HostTimeouts.ClientSend.TotalSeconds);
            }
            if (HostTimeouts.ClientReceive != InfiniteTimeout)
            {
                builder.AppendFormat(", Host client receive timeout {0}s", HostTimeouts.ClientReceive.TotalSeconds);
            }
            if (ClientTimeouts.SendAndReceive != InfiniteTimeout)
            {
                builder.AppendFormat(", Client send and receive timeout {0}s", HostTimeouts.ClientSend.TotalSeconds);
            }

            return builder.ToString();
        }
    }
}
