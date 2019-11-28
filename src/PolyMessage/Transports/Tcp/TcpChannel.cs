﻿using System;
using System.ComponentModel;
using System.IO;
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
                _stream.Dispose();
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

        // TODO: add async version
        private void EnsureConnected()
        {
            if (_stream != null)
                return;

            try
            {
                OpenConnection();
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

        private void OpenConnection()
        {
            if (!_tcpClient.Connected)
            {
                _tcpClient.Connect(_tcpTransport.Address.Host, _tcpTransport.Address.Port);
            }

            _stream = _tcpClient.GetStream();
            if (_tcpTransport.Settings.TlsProtocol != SslProtocols.None)
            {
                InitTls();
            }

            _tcpClient.NoDelay = _tcpTransport.Settings.NoDelay;
            Uri localAddress = new Uri($"tcp://{_tcpClient.Client.LocalEndPoint}");
            Uri remoteAddress = new Uri($"tcp://{_tcpClient.Client.RemoteEndPoint}");
            _connection.SetOpened(localAddress, remoteAddress);
        }

        private void InitTls()
        {
            TcpSettings settings = _tcpTransport.Settings;
            SslStream secureStream;
            if (_isServer)
            {
                secureStream = InitServerTls(settings);
            }
            else
            {
                secureStream = InitClientTls(settings);
            }

            _stream = secureStream;
        }

        private SslStream InitServerTls(TcpSettings settings)
        {
            if (settings.TlsServerCertificate == null)
                throw new InvalidOperationException("TLS Server certificate needs to be set.");

            _logger.LogTrace("Initializing {0} on the server using certificate {1} ...",
                settings.TlsProtocol, settings.TlsServerCertificate.Subject);
            SslStream secureStream = new SslStream(_stream, leaveInnerStreamOpen: false);
            secureStream.AuthenticateAsServer(settings.TlsServerCertificate, false, settings.TlsProtocol, true);
            _logger.LogTrace("Initialized {0} on the server with cipher {1}, hash {2}, key exchange {3}.",
                secureStream.SslProtocol, secureStream.CipherAlgorithm, secureStream.HashAlgorithm, secureStream.KeyExchangeAlgorithm);

            return secureStream;
        }

        private SslStream InitClientTls(TcpSettings settings)
        {
            _logger.LogTrace("Initializing {0} on the client ...", settings.TlsProtocol);
            SslStream secureStream = new SslStream(_stream, leaveInnerStreamOpen: false, settings.TlsClientRemoteCertificateValidationCallback);
            secureStream.AuthenticateAsClient(_tcpTransport.Address.Host, null, settings.TlsProtocol, true);
            _logger.LogTrace("Initialized {0} on the client with cipher {1}, hash {2}, key exchange {3}.",
                secureStream.SslProtocol, secureStream.CipherAlgorithm, secureStream.HashAlgorithm, secureStream.KeyExchangeAlgorithm);

            return secureStream;
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
                return _stream.Read(buffer, offset, count);
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
                return _stream.ReadAsync(buffer, offset, count, cancelToken);
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
                _stream.Write(buffer, offset, count);
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
                return _stream.WriteAsync(buffer, offset, count, cancelToken);
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
                _stream.Flush();
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
                return _stream.FlushAsync(cancelToken);
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
