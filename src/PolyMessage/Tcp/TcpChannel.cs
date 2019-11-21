using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage.Tcp
{
    internal sealed class TcpChannel : PolyChannel
    {
        private readonly PolyConnection _connection;
        // TCP
        private readonly TcpClient _tcpClient;
        private readonly TcpSettings _settings;
        private readonly Uri _connectAddress; // only available when the TCP client is not initially connected
        private NetworkStream _tcpStream;
        // close/dispose
        private bool _isDisposed;

        public TcpChannel(TcpClient tcpClient, TcpSettings settings)
        {
            _tcpClient = tcpClient;
            _settings = settings;
            _connection = new PolyConnection();
        }

        public TcpChannel(TcpClient tcpClient, TcpSettings settings, Uri connectAddress)
        {
            _tcpClient = tcpClient;
            _settings = settings;
            _connection = new PolyConnection();
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
                _connection.SetClosed();
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
                Dispose();
                throw PolyConnectionException.ConnectionClosed(ioException);
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
                Dispose();
                throw PolyConnectionException.ConnectionClosed(ioException);
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
                Dispose();
                throw PolyConnectionException.ConnectionClosed(ioException);
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
                Dispose();
                throw PolyConnectionException.ConnectionClosed(ioException);
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
                Dispose();
                throw PolyConnectionException.ConnectionClosed(ioException);
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
                Dispose();
                throw PolyConnectionException.ConnectionClosed(ioException);
            }
        }
    }
}
