using System;
using System.Net.Sockets;

namespace PolyMessage.Tcp
{
    public class TcpTransport : ITransport
    {
        private readonly Uri _address;

        public TcpTransport(Uri address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (!string.Equals(address.Scheme, "tcp", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Scheme should be TCP.");

            _address = address;
        }

        public string DisplayName => "TCP";

        public IListener CreateListener()
        {
            return new TcpListener(_address);
        }

        public IChannel CreateClient(IFormat format)
        {
            // TODO: initialize TCP client without connecting
            TcpClient tcpClient = new TcpClient(_address.Host, _address.Port);
            return new TcpChannel(tcpClient, format);
        }
    }
}
