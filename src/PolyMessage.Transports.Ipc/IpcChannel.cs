using System;
using System.Buffers;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Transports.Ipc.Messaging;

namespace PolyMessage.Transports.Ipc
{
    internal sealed class IpcChannel : PolyChannel
    {
        private readonly ILogger _logger;
        private readonly bool _isServer;
        private readonly ArrayPool<byte> _bufferPool;
        // IPC
        private readonly IpcTransport _ipcTransport;
        private readonly Protocol _protocol;
        private readonly PipeStream _pipeStream;
        private MemoryMappedFile _mmf;
        private MemoryMappedViewStream _mmfStream;
        private InMemoryStream _dataStream;
        private string _mmfName;
        // close/dispose
        private bool _isDisposed;

        public IpcChannel(
            PipeStream pipeStream, IpcTransport ipcTransport, bool isServer,
            Protocol protocol, ArrayPool<byte> bufferPool, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _isServer = isServer;
            _bufferPool = bufferPool;
            // IPC
            _ipcTransport = ipcTransport;
            _protocol = protocol;
            _pipeStream = pipeStream;
        }

        protected override void DoDispose(bool isDisposing)
        {
            if (_isDisposed)
                return;

            if (isDisposing)
            {
                _pipeStream.Dispose();
                _mmfStream?.Dispose();
                _mmf?.Dispose();
                _dataStream?.Dispose();
                MutableConnection.SetClosed();
                _isDisposed = true;
                _logger.LogDebug("Disconnected pipe {0} and MMF {1}.", _ipcTransport.Address, _mmfName ?? "(empty)");
            }

            base.DoDispose(isDisposing);
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException($"{_ipcTransport.DisplayName} channel is already disposed.");
        }

        private void EnsureOpened()
        {
            if (_mmf == null)
                throw new InvalidOperationException($"{_ipcTransport.DisplayName} channel is not opened.");
        }

        public override async Task OpenAsync()
        {
            EnsureNotDisposed();

            if (!_isServer)
            {
                NamedPipeClientStream clientPipeStream = (NamedPipeClientStream) _pipeStream;
                if (!clientPipeStream.IsConnected)
                {
                    _logger.LogError("Connecting client pipe to {0}...", _ipcTransport.Address);
                    // TODO: consider including connect timeout for IPC and TCP transports
                    await clientPipeStream.ConnectAsync();
                    clientPipeStream.ReadMode = PipeTransmissionMode.Byte;
                    _logger.LogError("Connected client pipe to {0}.", _ipcTransport.Address);
                }
            }

            _mmfName = await ExchangeMmfName().ConfigureAwait(false);

            // TODO: for mmf capacity and size use a setting
            _mmf = MemoryMappedFile.CreateOrOpen(_mmfName, _ipcTransport.MessageBufferSettings.MaxSize, MemoryMappedFileAccess.ReadWrite);
            _mmfStream = _mmf.CreateViewStream(offset: 0, size: _ipcTransport.MessageBufferSettings.MaxSize);

            _dataStream = new InMemoryStream(_logger, _mmfStream, _bufferPool, _ipcTransport.MessageBufferSettings.InitialSize);

            _logger.LogDebug("Connected to pipe {0} and MMF {1}.", _ipcTransport.Address, _mmfName);
            MutableConnection.SetOpened(_ipcTransport.Address, _ipcTransport.Address);
        }

        private async Task<string> ExchangeMmfName()
        {
            string mmfName;

            if (_isServer)
            {
                mmfName = Guid.NewGuid().ToString();
                await _protocol.SendMmfName(mmfName, _bufferPool, _pipeStream, "Init", CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                mmfName = await _protocol.ReceiveMmfName(_bufferPool, _pipeStream, "Init", CancellationToken.None).ConfigureAwait(false);
            }

            return mmfName;
        }

        public override void Close()
        {
            Dispose();
        }

        public override async Task<object> Receive(PolyFormatter formatter, string origin, CancellationToken ct)
        {
            EnsureNotDisposed();
            EnsureOpened();

            // TODO: try catch errors
            return await _protocol.ReceiveMessage(formatter, _bufferPool, _pipeStream, _dataStream, _mmfStream, origin, ct).ConfigureAwait(false);
        }

        public override async Task Send(object message, PolyFormatter formatter, string origin, CancellationToken ct)
        {
            EnsureNotDisposed();
            EnsureOpened();

            // TODO: try catch errors
            await _protocol.SendMessage(message, formatter, _bufferPool, _pipeStream, _dataStream, _mmfStream, origin, ct).ConfigureAwait(false);
        }
    }
}
