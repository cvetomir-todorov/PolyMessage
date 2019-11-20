using System;
using System.IO;
using System.Net.Sockets;

namespace PolyMessage.Tcp
{
    internal sealed class TcpChannel : PolyChannel
    {
        private readonly string _displayName;
        private readonly TcpClient _tcpClient;
        private readonly TcpSettings _settings;
        // only available when the TCP client is not initially connected
        private readonly Uri _connectAddress;
        private Stream _tcpStream;
        private Uri _localAddress;
        private Uri _remoteAddress;
        private bool _isDisposed;

        public TcpChannel(string displayName, TcpClient tcpClient, TcpSettings settings)
        {
            _displayName = displayName;
            _tcpClient = tcpClient;
            _settings = settings;
        }

        public TcpChannel(string displayName, TcpClient tcpClient, TcpSettings settings, Uri connectAddress)
        {
            _displayName = displayName;
            _tcpClient = tcpClient;
            _settings = settings;
            _connectAddress = connectAddress;
        }

        protected override void DoDispose(bool isDisposing)
        {
            if (_isDisposed)
                return;

            if (isDisposing)
            {
                _tcpStream.Dispose();
                _tcpClient.Close();
                _tcpClient.Dispose();
                _isDisposed = true;
            }

            base.DoDispose(isDisposing);
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException("TCP channel is already disposed.");
        }

        private void EnsureConnected()
        {
            if (_tcpStream == null)
            {
                if (!_tcpClient.Connected)
                {
                    _tcpClient.Connect(_connectAddress.Host, _connectAddress.Port);
                }

                _tcpStream = _tcpClient.GetStream();
                _tcpClient.NoDelay = _settings.NoDelay;
                _localAddress = new Uri($"tcp://{_tcpClient.Client.LocalEndPoint}");
                _remoteAddress = new Uri($"tcp://{_tcpClient.Client.RemoteEndPoint}");
            }
        }

        public override string DisplayName => _displayName;

        public override void Open()
        {
            EnsureNotDisposed();
            EnsureConnected();
        }

        public override Uri LocalAddress
        {
            get
            {
                EnsureNotDisposed();
                EnsureConnected();
                return _localAddress;
            }
        }

        public override Uri RemoteAddress
        {
            get
            {
                EnsureNotDisposed();
                EnsureConnected();
                return _remoteAddress;
            }
        }

        public override Stream Stream
        {
            get
            {
                EnsureNotDisposed();
                EnsureConnected();
                return _tcpStream;
            }
        }
    }
}
