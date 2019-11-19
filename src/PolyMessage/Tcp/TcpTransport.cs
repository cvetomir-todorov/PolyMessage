using System;
using System.Net.Sockets;
using System.Threading;

namespace PolyMessage.Tcp
{
    // TODO: use timeouts
    public class TcpTransport : PolyTransport
    {
        private readonly Uri _address;
        private readonly TcpSettings _settings;
        // timeouts
        public static readonly TimeSpan InfiniteTimeout = Timeout.InfiniteTimeSpan;
        private TimeSpan _receiveTimeout;
        private TimeSpan _sendTimeout;

        public TcpTransport(Uri address)
            : this(address, receiveTimeout: InfiniteTimeout, sendTimeout: InfiniteTimeout)
        {}

        public TcpTransport(Uri address, TimeSpan receiveTimeout, TimeSpan sendTimeout)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (!string.Equals(address.Scheme, "tcp", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Scheme should be TCP.");

            _address = address;
            _settings = new TcpSettings();
            _receiveTimeout = receiveTimeout;
            _sendTimeout = sendTimeout;
        }

        public TcpSettings Settings => _settings;

        public override string DisplayName => "TCP";

        public override Uri Address => _address;

        public override TimeSpan ReceiveTimeout
        {
            get { return _receiveTimeout;}
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentException("Timeout should be greater than zero.");
                _receiveTimeout = value;
            }
        }

        public override TimeSpan SendTimeout
        {
            get { return _sendTimeout; }
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentException("Timeout should be greater than zero.");
                _sendTimeout = value;
            }
        }

        public override PolyListener CreateListener()
        {
            return new TcpListener(DisplayName, _address, _settings);
        }

        public override PolyChannel CreateClient()
        {
            // TODO: initialize TCP client without connecting
            TcpClient tcpClient = new TcpClient(_address.Host, _address.Port);
            return new TcpChannel(DisplayName, tcpClient, _settings);
        }
    }
}
