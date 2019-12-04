using System;
using System.Net.Sockets;
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
    }
}
