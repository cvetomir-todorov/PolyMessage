using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage.Tcp
{
    internal sealed class TcpChannel : IChannel
    {
        private readonly TcpClient _tcpClient;
        private readonly Stream _tcpStream;
        private readonly IFormat _format;
        private bool _isDisposed;

        public TcpChannel(TcpClient tcpClient, IFormat format)
        {
            _tcpClient = tcpClient;
            _tcpStream = _tcpClient.GetStream();
            _format = format;
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

        public Task Send(string message, CancellationToken cancelToken)
        {
            EnsureNotDisposed();

            // TODO: use timeout to avoid hanging when server crashes
            _format.WriteToStream(message, _tcpStream, cancelToken);
            return Task.CompletedTask;
        }

        public async Task<string> Receive(CancellationToken cancelToken)
        {
            EnsureNotDisposed();

            // TODO: use timeout to avoid hanging when server crashes
            string message = await _format.ReadFromStream(_tcpStream, cancelToken).ConfigureAwait(false);
            return message;
        }
    }
}
