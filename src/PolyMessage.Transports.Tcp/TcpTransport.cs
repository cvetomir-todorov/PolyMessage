using System;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PolyMessage.Transports.Tcp
{
    public class TcpTransport : PolyTransport
    {
        private readonly ILoggerFactory _loggerFactory;

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
            _loggerFactory = loggerFactory;
        }

        public override string DisplayName => "TCP";

        public override Uri Address { get; }

        public TcpSettings Settings { get; }

        public override PolyListener CreateListener()
        {
            EnsureReadyForCommunication();
            return new TcpListener(this, BufferPool, MessageMetadata, _loggerFactory);
        }

        public override PolyChannel CreateClient()
        {
            EnsureReadyForCommunication();
            TcpClient tcpClient = new TcpClient();
            return new TcpChannel(tcpClient, this, isServer: false, BufferPool, MessageMetadata, _loggerFactory);
        }

        private void EnsureReadyForCommunication()
        {
            if (BufferPool == null)
                throw new InvalidOperationException("Buffer pool needs to be initialized.");
            if (MessageMetadata == null)
                throw new InvalidOperationException("Message metadata needs to be initialized.");
        }

        public override string GetSettingsInfo()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendFormat("Send buffer size {0}", Settings.SendBufferSize);
            builder.AppendFormat(", Receive buffer size {0}", Settings.ReceiveBufferSize);
            builder.AppendFormat(", NoDelay {0}", Settings.NoDelay ? "enabled" : "disabled");
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

            return builder.ToString();
        }
    }
}
