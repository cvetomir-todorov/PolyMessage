using System;
using System.Net.Sockets;

namespace PolyMessage.Tcp
{
    public class TcpTransport : PolyTransport
    {
        private readonly Uri _address;
        private readonly TcpSettings _settings;

        public TcpTransport(Uri address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (!string.Equals(address.Scheme, "tcp", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Scheme should be TCP.");

            _address = address;
            _settings = new TcpSettings();
        }

        public TcpSettings Settings => _settings;

        public override string DisplayName => "TCP";

        public override Uri Address => _address;

        public override PolyListener CreateListener()
        {
            return new TcpListener(DisplayName, _address, _settings);
        }

        public override PolyChannel CreateClient()
        {
            TcpClient tcpClient = new TcpClient();
            return new TcpChannel(DisplayName, tcpClient, _settings, _address);
        }
    }
}
