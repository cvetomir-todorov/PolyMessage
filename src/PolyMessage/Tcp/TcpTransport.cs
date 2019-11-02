using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using PolyMessage.Formats;
using PolyMessage.Transports;

namespace PolyMessage.Tcp
{
    public class TcpTransport : ITransport
    {
        private readonly Uri _address;
        private TcpListener _tcpListener;
        private bool _isDisposed;

        public TcpTransport(Uri address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (!string.Equals(address.Scheme, "tcp", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Scheme should be TCP.");

            _address = address;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _tcpListener?.Stop();
            _isDisposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException("TCP transport is already disposed.");
        }

        public string DisplayName => "TCP";

        // TODO: create a listener with the 3 methods for start/accept/stop

        public Task PrepareAccepting()
        {
            EnsureNotDisposed();

            IPAddress hostname = IPAddress.Parse(_address.Host);
            _tcpListener = new TcpListener(hostname, _address.Port);
            _tcpListener.Start();
            return Task.CompletedTask;
        }

        public async Task<IChannel> AcceptClient(IFormat format)
        {
            EnsureNotDisposed();

            TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
            return new TcpChannel(tcpClient, format);
        }

        public void StopAccepting()
        {
            Dispose();
        }

        public IChannel CreateClient(IFormat format)
        {
            EnsureNotDisposed();

            // TODO: initialize TCP client without connecting
            TcpClient tcpClient = new TcpClient(_address.Host, _address.Port);
            return new TcpChannel(tcpClient, format);
        }
    }
}
