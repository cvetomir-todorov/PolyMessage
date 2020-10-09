using System;
using System.Buffers;
using System.IO.Pipes;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Transports.Ipc.Messaging;

namespace PolyMessage.Transports.Ipc
{
    internal sealed class IpcListener : PolyListener
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ArrayPool<byte> _bufferPool;
        // IPC
        private readonly IpcTransport _ipcTransport;
        private readonly Protocol _protocol;
        private NamedPipeServerStream _currentServerPipeStream;
        // close/dispose
        private bool _isDisposed;

        public IpcListener(IpcTransport ipcTransport, Protocol protocol, ArrayPool<byte> bufferPool, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _bufferPool = bufferPool;
            // IPC
            _ipcTransport = ipcTransport;
            _protocol = protocol;
        }

        protected override void DoDispose(bool isDisposing)
        {
            if (_isDisposed)
                return;

            if (isDisposing)
            {
                _currentServerPipeStream?.Dispose();
                _isDisposed = true;
            }

            base.DoDispose(isDisposing);
        }

        public override async Task<Func<PolyChannel>> AcceptClient()
        {
            // TODO: get from IPC settings via transport:
            // in/out buffer sizes - may not need to be specified because we send limited and only protocol data on the pipe
            _currentServerPipeStream = new NamedPipeServerStream(
                _ipcTransport.Address.PathAndQuery, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 10, 10);

            await _currentServerPipeStream.WaitForConnectionAsync();
            NamedPipeServerStream local = _currentServerPipeStream;
            return () => CreateClientChannel(local);
        }

        private PolyChannel CreateClientChannel(NamedPipeServerStream serverPipeStream)
        {
            return new IpcChannel(serverPipeStream, _ipcTransport, isServer: true, _protocol, _bufferPool, _loggerFactory);
        }

        public override void StopAccepting()
        {
            Dispose();
        }
    }
}
