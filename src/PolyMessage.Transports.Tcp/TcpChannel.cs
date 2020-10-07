using System;
using System.Buffers;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Exceptions;
using PolyMessage.Metadata;

namespace PolyMessage.Transports.Tcp
{
    internal sealed class TcpChannel : PolyChannel
    {
        private readonly ILogger _logger;
        private readonly bool _isServer;
        private readonly ArrayPool<byte> _bufferPool;
        private readonly IMessageMetadata _messageMetadata;
        // TCP
        private readonly TcpClient _tcpClient;
        private readonly TcpTransport _tcpTransport;
        private Stream _stream;
        // messaging
        private LengthPrefixStream _lengthPrefixStream;
        private LengthPrefixProtocol _lengthPrefixProtocol;
        // close/dispose
        private bool _isDisposed;

        public TcpChannel(
            TcpClient tcpClient, TcpTransport tcpTransport, bool isServer,
            ArrayPool<byte> bufferPool, IMessageMetadata messageMetadata, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _isServer = isServer;
            _bufferPool = bufferPool;
            _messageMetadata = messageMetadata;
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
                MutableConnection.SetClosed();
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

        public override async Task OpenAsync()
        {
            EnsureNotDisposed();

            try
            {
                await OpenConnection();
            }
            catch (Win32Exception win32Exception) // also catches SocketException
            {
                throw new PolyConnectionOpenException(_tcpTransport, win32Exception);
            }
            catch (IOException ioException)
            {
                throw new PolyConnectionOpenException(_tcpTransport, ioException);
            }
            catch (AuthenticationException authenticationException)
            {
                throw new PolyConnectionOpenException(_tcpTransport, authenticationException);
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
            _lengthPrefixStream = new LengthPrefixStream(_logger, _stream, _bufferPool, _tcpTransport.MessageBufferSettings.InitialSize);
            _lengthPrefixProtocol = new LengthPrefixProtocol(_logger, _messageMetadata, _tcpTransport);

            _tcpClient.NoDelay = _tcpTransport.Settings.NoDelay;
            _tcpClient.SendBufferSize = _tcpTransport.Settings.SendBufferSize;
            _tcpClient.ReceiveBufferSize = _tcpTransport.Settings.ReceiveBufferSize;

            Uri localAddress = new Uri($"tcp://{_tcpClient.Client.LocalEndPoint}");
            Uri remoteAddress = new Uri($"tcp://{_tcpClient.Client.RemoteEndPoint}");
            MutableConnection.SetOpened(localAddress, remoteAddress);
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

        public override async Task<object> Receive(PolyFormatter formatter, string origin, CancellationToken ct)
        {
            EnsureNotDisposed();
            EnsureConnected();
            try
            {
                return await _lengthPrefixProtocol.Receive(formatter, _lengthPrefixStream, origin, ct);
            }
            catch (IOException ioException)
            {
                PolyException polyException = TryHandleIOException(ioException);
                if (polyException != null)
                    throw polyException;
                throw;
            }
        }

        public override async Task Send(object message, PolyFormatter formatter, string origin, CancellationToken ct)
        {
            EnsureNotDisposed();
            EnsureConnected();
            try
            {
                await _lengthPrefixProtocol.Send(message, formatter, _lengthPrefixStream, origin, ct);
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

            Dispose();

            switch (socketException.SocketErrorCode)
            {
                case SocketError.ConnectionAborted:
                    return new PolyConnectionClosedException(PolyConnectionCloseReason.ConnectionAborted, _tcpTransport, ioException);
                case SocketError.ConnectionReset:
                    return new PolyConnectionClosedException(PolyConnectionCloseReason.ConnectionReset, _tcpTransport, ioException);
                default:
                    return new PolyConnectionClosedException(PolyConnectionCloseReason.Unexpected, _tcpTransport, ioException);
            }
        }
    }
}
