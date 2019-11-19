using System;
using System.IO;
using System.Net.Sockets;

namespace PolyMessage.Tcp
{
    internal sealed class TcpChannel : PolyChannel
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

        public override string DisplayName => _displayName;

        public override Stream Stream
        {
            get
            {
                EnsureNotDisposed();
                return _tcpStream;
            }
        }
    }
}
