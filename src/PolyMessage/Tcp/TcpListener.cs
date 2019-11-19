using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DotNetTcpListener = System.Net.Sockets.TcpListener;

namespace PolyMessage.Tcp
{
    internal sealed class TcpListener : PolyListener
    {
        private readonly string _displayName;
        private readonly Uri _address;
        private readonly TcpSettings _settings;
        private DotNetTcpListener _tcpListener;
        private bool _isDisposed;

        public TcpListener(string displayName, Uri address, TcpSettings settings)
        {
            _displayName = displayName;
            _address = address;
            _settings = settings;
        }

        protected override void DoDispose(bool isDisposing)
        {
            if (_isDisposed)
                return;

            if (isDisposing)
            {
                _tcpListener?.Stop();
                _isDisposed = true;
            }

            base.DoDispose(isDisposing);
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException("TCP transport is already disposed.");
        }

        public override string DisplayName => _displayName;

        public override Task PrepareAccepting()
        {
            EnsureNotDisposed();

            IPAddress hostname = IPAddress.Parse(_address.Host);
            _tcpListener = new DotNetTcpListener(hostname, _address.Port);
            _tcpListener.Start();
            return Task.CompletedTask;
        }

        public override async Task<PolyChannel> AcceptClient()
        {
            EnsureNotDisposed();

            TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
            return new TcpChannel(_displayName, tcpClient, _settings);
        }

        public override void StopAccepting()
        {
            Dispose();
        }
    }
}
