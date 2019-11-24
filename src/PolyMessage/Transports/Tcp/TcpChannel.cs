using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage.Transports.Tcp
{
    internal sealed class TcpChannel : PolyChannel
    {
        private readonly PolyConnection _connection;
        // TCP
        private readonly TcpClient _tcpClient;
        private readonly TcpTransport _tcpTransport;
        private NetworkStream _tcpStream;
        // close/dispose
        private bool _isDisposed;

        public TcpChannel(TcpClient tcpClient, TcpTransport tcpTransport)
        {
            _tcpClient = tcpClient;
            _tcpTransport = tcpTransport;
            _connection = new PolyConnection();
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
                _connection.SetClosed();
                _isDisposed = true;
            }

            base.DoDispose(isDisposing);
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException($"{_tcpTransport.DisplayName} channel is already disposed.");
        }

        private void EnsureConnected()
        {
            if (_tcpStream == null)
            {
                if (!_tcpClient.Connected)
                {
                    _tcpClient.Connect(_tcpTransport.Address.Host, _tcpTransport.Address.Port);
                }

                _tcpStream = _tcpClient.GetStream();
                _tcpClient.NoDelay = _tcpTransport.Settings.NoDelay;
                Uri localAddress = new Uri($"tcp://{_tcpClient.Client.LocalEndPoint}");
                Uri remoteAddress = new Uri($"tcp://{_tcpClient.Client.RemoteEndPoint}");
                _connection.SetOpened(localAddress, remoteAddress);
            }
        }

        public override PolyConnection Connection => _connection;

        public override void Open()
        {
            EnsureNotDisposed();
            EnsureConnected();
        }

        public override void Close()
        {
            Dispose();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            EnsureNotDisposed();
            EnsureConnected();
            try
            {
                return _tcpStream.Read(buffer, offset, count);
            }
            catch (IOException ioException)
            {
                PolyException polyException = TryHandleIOException(ioException);
                if (polyException != null)
                    throw polyException;
                throw;
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancelToken)
        {
            EnsureNotDisposed();
            EnsureConnected();
            try
            {
                return _tcpStream.ReadAsync(buffer, offset, count, cancelToken);
            }
            catch (IOException ioException)
            {
                PolyException polyException = TryHandleIOException(ioException);
                if (polyException != null)
                    throw polyException;
                throw;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureNotDisposed();
            EnsureConnected();
            try
            {
                _tcpStream.Write(buffer, offset, count);
            }
            catch (IOException ioException)
            {
                PolyException polyException = TryHandleIOException(ioException);
                if (polyException != null)
                    throw polyException;
                throw;
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancelToken)
        {
            EnsureNotDisposed();
            EnsureConnected();
            try
            {
                return _tcpStream.WriteAsync(buffer, offset, count, cancelToken);
            }
            catch (IOException ioException)
            {
                PolyException polyException = TryHandleIOException(ioException);
                if (polyException != null)
                    throw polyException;
                throw;
            }
        }

        public override void Flush()
        {
            EnsureNotDisposed();
            EnsureConnected();
            try
            {
                _tcpStream.Flush();
            }
            catch (IOException ioException)
            {
                PolyException polyException = TryHandleIOException(ioException);
                if (polyException != null)
                    throw polyException;
                throw;
            }
        }

        public override Task FlushAsync(CancellationToken cancelToken)
        {
            EnsureNotDisposed();
            EnsureConnected();
            try
            {
                return _tcpStream.FlushAsync(cancelToken);
            }
            catch (IOException ioException)
            {
                PolyException polyException = TryHandleIOException(ioException);
                if (polyException != null)
                    throw polyException;
                throw;
            }
        }

        private PolyException TryHandleIOException(IOException ioException)
        {
            SocketException socketException = ioException.InnerException as SocketException;
            if (socketException == null)
            {
                return null;
            }

            switch (socketException.SocketErrorCode)
            {
                case SocketError.TimedOut:
                    Dispose();
                    return new PolyConnectionClosedException(PolyConnectionCloseReason.RemoteTimedOut, _tcpTransport, ioException);
                case SocketError.ConnectionAborted:
                    Dispose();
                    return new PolyConnectionClosedException(PolyConnectionCloseReason.RemoteAbortedConnection, _tcpTransport, ioException);
                default:
                    return null;
            }
        }
    }
}
