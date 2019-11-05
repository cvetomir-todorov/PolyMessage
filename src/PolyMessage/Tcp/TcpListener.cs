using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DotNetTcpListener = System.Net.Sockets.TcpListener;

namespace PolyMessage.Tcp
{
    internal sealed class TcpListener : IListener
    {
        private readonly string _displayName;
        private readonly Uri _address;
        private DotNetTcpListener _tcpListener;
        private bool _isDisposed;

        public TcpListener(string displayName, Uri address)
        {
            _displayName = displayName;
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

        public string DisplayName => _displayName;

        public Task PrepareAccepting()
        {
            EnsureNotDisposed();

            IPAddress hostname = IPAddress.Parse(_address.Host);
            _tcpListener = new DotNetTcpListener(hostname, _address.Port);
            _tcpListener.Start();
            return Task.CompletedTask;
        }

        public async Task<IChannel> AcceptClient()
        {
            EnsureNotDisposed();

            TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
            return new TcpChannel(_displayName, tcpClient);
        }

        public void StopAccepting()
        {
            Dispose();
        }
    }
}
