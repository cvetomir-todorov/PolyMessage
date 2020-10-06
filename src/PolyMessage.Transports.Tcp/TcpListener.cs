using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Exceptions;
using PolyMessage.Metadata;
using DotNetTcpListener = System.Net.Sockets.TcpListener;

namespace PolyMessage.Transports.Tcp
{
    internal sealed class TcpListener : PolyListener
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ArrayPool<byte> _bufferPool;
        private readonly IReadOnlyMessageMetadata _messageMetadata;
        // TCP
        private readonly TcpTransport _tcpTransport;
        private DotNetTcpListener _tcpListener;
        // stop/dispose
        private bool _isDisposed;

        public TcpListener(TcpTransport tcpTransport, ArrayPool<byte> bufferPool, IReadOnlyMessageMetadata messageMetadata, ILoggerFactory loggerFactory)
        {
            _bufferPool = bufferPool;
            _messageMetadata = messageMetadata;
            _loggerFactory = loggerFactory;
            _tcpTransport = tcpTransport;
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
                throw new InvalidOperationException($"{_tcpTransport.DisplayName} listener is already disposed.");
        }

        public override void PrepareAccepting()
        {
            EnsureNotDisposed();

            IPAddress hostname = IPAddress.Parse(_tcpTransport.Address.Host);
            _tcpListener = new DotNetTcpListener(hostname, _tcpTransport.Address.Port);
            _tcpListener.Start();
        }

        public override async Task<Func<PolyChannel>> AcceptClient()
        {
            EnsureNotDisposed();

            try
            {
                TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                return () => CreateClientChannel(tcpClient);
            }
            catch (ObjectDisposedException objectDisposedException) when (objectDisposedException.ObjectName == typeof(Socket).FullName)
            {
                throw new PolyListenerStoppedException(_tcpTransport, objectDisposedException);
            }
        }

        private PolyChannel CreateClientChannel(TcpClient tcpClient)
        {
            return new TcpChannel(tcpClient, _tcpTransport, isServer: true, _bufferPool, _messageMetadata, _loggerFactory);
        }

        public override void StopAccepting()
        {
            Dispose();
        }
    }
}
