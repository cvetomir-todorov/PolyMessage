using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using PolyMessage.Messaging;

namespace PolyMessage.Transports.Tcp
{
    public class TcpTransport : PolyTransport
    {
        private readonly ILoggerFactory _loggerFactory;
        private bool _isInitialized;
        private ArrayPool<byte> _bufferPool;

        public TcpTransport(Uri address, ILoggerFactory loggerFactory)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (!string.Equals(address.Scheme, "tcp", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Uri scheme should be TCP.", nameof(address));
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _loggerFactory = loggerFactory;
            Address = address;
            DisplayName = "TCP";
            Settings = new TcpSettings();
        }

        public TcpSettings Settings { get; }

        public override PolyListener CreateListener()
        {
            Initialize();
            return new TcpListener(this, _bufferPool, MessageMetadata, _loggerFactory);
        }

        public override PolyChannel CreateClient()
        {
            Initialize();
            TcpClient tcpClient = new TcpClient();
            return new TcpChannel(tcpClient, this, isServer: false, _bufferPool, MessageMetadata, _loggerFactory);
        }

        private void Initialize()
        {
            if (_isInitialized)
                return;
            if (MessageMetadata == null)
                throw new InvalidOperationException("Message metadata needs to be initialized.");

            _bufferPool = ArrayPool<byte>.Create(
                maxArrayLength: MessageBufferSettings.MaxSize,
                maxArraysPerBucket: MessageBufferSettings.MaxArraysPerBucket);
            _isInitialized = true;
        }

        public override IEnumerable<MessageInfo> GetMessageTypes()
        {
            return new[] {new MessageInfo(typeof(PolyHeader), PolyHeader.TypeID)};
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
