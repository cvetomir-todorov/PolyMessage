using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PolyMessage.Transports.Tcp
{
    internal sealed class TcpChannel : PolyChannel
    {
        private readonly ILogger _logger;
        private readonly bool _isServer;
        private readonly PolyConnection _connection;
        // TCP
        private readonly TcpClient _tcpClient;
        private readonly TcpTransport _tcpTransport;
        private Stream _stream;
        // close/dispose
        private bool _isDisposed;

        public TcpChannel(TcpClient tcpClient, TcpTransport tcpTransport, bool isServer, ILogger logger)
        {
            _logger = logger;
            _isServer = isServer;
            _connection = new PolyConnection();
            // TCP
            _tcpClient = tcpClient;
            _tcpTransport = tcpTransport;
        }

        protected override void DoDispose(bool isDisposing)
        {
            if (_isDisposed)
                return;

            if (isDisposing)
            {
                EndPoint remoteAddress = _tcpClient.Client.RemoteEndPoint;
                _stream.Dispose();
                _tcpClient.Close();
                _tcpClient.Dispose();
                _connection.SetClosed();
                _isDisposed = true;
                _logger.LogDebug("Disconnected from tcp://{0}.", remoteAddress);
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
            if (_stream == null)
                throw new InvalidOperationException($"{_tcpTransport.DisplayName} channel is not opened.");
        }

        public override PolyTransport Transport => _tcpTransport;

        public override PolyConnection Connection => _connection;

        public override async Task OpenAsync()
        {
            EnsureNotDisposed();

            try
            {
                await OpenConnection();
            }
            catch (Win32Exception win32Exception) // also catches SocketException
            {
                throw new PolyOpenConnectionException(_tcpTransport, win32Exception);
            }
            catch (IOException ioException)
            {
                throw new PolyOpenConnectionException(_tcpTransport, ioException);
            }
            catch (AuthenticationException authenticationException)
            {
                throw new PolyOpenConnectionException(_tcpTransport, authenticationException);
            }
        }

        private async Task OpenConnection()
        {
            if (!_tcpClient.Connected)
            {
                _logger.LogDebug("Connecting to tcp://{0}:{1}...", _tcpTransport.Address.Host, _tcpTransport.Address.Port);
                await _tcpClient.ConnectAsync(_tcpTransport.Address.Host, _tcpTransport.Address.Port).ConfigureAwait(false);
            }
            _logger.LogDebug("Connected to tcp://{0}.", _tcpClient.Client.RemoteEndPoint);

            _stream = _tcpClient.GetStream();
            if (_tcpTransport.Settings.TlsProtocol != SslProtocols.None)
            {
                await InitTls().ConfigureAwait(false);
            }

            _tcpClient.NoDelay = _tcpTransport.Settings.NoDelay;
            _tcpClient.SendBufferSize = _tcpTransport.Settings.SendBufferSize;
            _tcpClient.ReceiveBufferSize = _tcpTransport.Settings.ReceiveBufferSize;

            Uri localAddress = new Uri($"tcp://{_tcpClient.Client.LocalEndPoint}");
            Uri remoteAddress = new Uri($"tcp://{_tcpClient.Client.RemoteEndPoint}");
            _connection.SetOpened(localAddress, remoteAddress);
        }

        private async Task InitTls()
        {
            TcpSettings settings = _tcpTransport.Settings;
            SslStream secureStream;
            if (_isServer)
            {
                secureStream = await InitServerTls(settings).ConfigureAwait(false);
            }
            else
            {
                secureStream = await InitClientTls(settings).ConfigureAwait(false);
            }

            _stream = secureStream;
        }

        private async Task<SslStream> InitServerTls(TcpSettings settings)
        {
            if (settings.TlsServerCertificate == null)
                throw new InvalidOperationException("TLS Server certificate needs to be set.");

            _logger.LogDebug("Initializing {0} server-side using certificate {1}...",
                settings.TlsProtocol, settings.TlsServerCertificate.Subject);

            SslStream secureStream = new SslStream(_stream, leaveInnerStreamOpen: false);
            await secureStream.AuthenticateAsServerAsync(settings.TlsServerCertificate, false, settings.TlsProtocol, true).ConfigureAwait(false);

            _logger.LogDebug("Initialized {0} server-side using certificate {1}, cipher {2}, hash {3} and key exchange {4}.",
                secureStream.SslProtocol, secureStream.LocalCertificate.Subject,
                secureStream.CipherAlgorithm, secureStream.HashAlgorithm, secureStream.KeyExchangeAlgorithm);

            return secureStream;
        }

        private async Task<SslStream> InitClientTls(TcpSettings settings)
        {
            _logger.LogDebug("Initializing {0} client-side...", settings.TlsProtocol);

            SslStream secureStream = new SslStream(_stream, leaveInnerStreamOpen: false, settings.TlsClientRemoteCertificateValidationCallback);
            await secureStream.AuthenticateAsClientAsync(_tcpTransport.Address.Host, null, settings.TlsProtocol, true).ConfigureAwait(false);

            if (secureStream.RemoteCertificate == null)
                throw new InvalidOperationException("TLS Server certificate was not set after client authenticated.");

            _logger.LogDebug("Initialized {0} client-side using remote certificate {1}, cipher {2}, hash {3} and key exchange {4}.",
                secureStream.SslProtocol, secureStream.RemoteCertificate.Subject,
                secureStream.CipherAlgorithm, secureStream.HashAlgorithm, secureStream.KeyExchangeAlgorithm);

            return secureStream;
        }

        public override void Close()
        {
            Dispose();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            EnsureNotDisposed();
            EnsureConnected();
            try
            {
                return await _stream.ReadAsync(buffer, offset, count, ct);
            }
            catch (IOException ioException)
            {
                PolyException polyException = TryHandleIOException(ioException);
                if (polyException != null)
                    throw polyException;
                throw;
            }
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            EnsureNotDisposed();
            EnsureConnected();
            try
            {
                await _stream.WriteAsync(buffer, offset, count, ct);
            }
            catch (IOException ioException)
            {
                PolyException polyException = TryHandleIOException(ioException);
                if (polyException != null)
                    throw polyException;
                throw;
            }
        }

        public override async Task FlushAsync(CancellationToken ct)
        {
            EnsureNotDisposed();
            EnsureConnected();
            try
            {
                await _stream.FlushAsync(ct);
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
                case SocketError.ConnectionReset:
                    Dispose();
                    return new PolyConnectionClosedException(PolyConnectionCloseReason.ConnectionReset, _tcpTransport, ioException);
                default:
                    return null;
            }
        }
    }
}
