using System;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace PolyMessage.Transports.Tcp
{
    public class TcpTransport : PolyTransport
    {
        private readonly Uri _address;
        private readonly TcpSettings _settings;
        private readonly ILogger _logger;

        public TcpTransport(Uri address, ILoggerFactory loggerFactory)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (!string.Equals(address.Scheme, "tcp", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Uri scheme should be TCP.");
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _address = address;
            _settings = new TcpSettings();
            _logger = loggerFactory.CreateLogger(GetType());
        }

        public TcpSettings Settings => _settings;

        public override string DisplayName => "TCP";

        public override Uri Address => _address;

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
