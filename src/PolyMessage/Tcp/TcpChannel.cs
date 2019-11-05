using System;
using System.IO;
using System.Net.Sockets;

namespace PolyMessage.Tcp
{
    internal sealed class TcpChannel : IChannel
    {
        private readonly string _displayName;
        private readonly TcpClient _tcpClient;
        private readonly Stream _tcpStream;
        private bool _isDisposed;

        public TcpChannel(string displayName, TcpClient tcpClient)
        {
            _displayName = displayName;
            _tcpClient = tcpClient;
            _tcpStream = _tcpClient.GetStream();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _tcpStream.Dispose();
            _tcpClient.Close();
            _tcpClient.Dispose();

            _isDisposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException("TCP channel is already disposed.");
        }

        public string DisplayName => _displayName;

        public Stream Stream
        {
            get
            {
                EnsureNotDisposed();
                return _tcpStream;
            }
        }
    }
}
