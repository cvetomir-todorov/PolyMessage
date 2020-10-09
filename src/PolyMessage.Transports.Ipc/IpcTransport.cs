using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipes;
using Microsoft.Extensions.Logging;
using PolyMessage.Transports.Ipc.Messaging;

namespace PolyMessage.Transports.Ipc
{
    public class IpcTransport : PolyTransport
    {
        private readonly ILoggerFactory _loggerFactory;
        private Protocol _protocol;
        private bool _isInitialized;
        private ArrayPool<byte> _bufferPool;

        public IpcTransport(Uri namedPipe, ILoggerFactory loggerFactory)
        {
            if (namedPipe == null)
                throw new ArgumentNullException(nameof(namedPipe));
            if (namedPipe.Scheme != Uri.UriSchemeNetPipe)
                throw new ArgumentException("Uri scheme should be net.pipe", nameof(namedPipe));
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _loggerFactory = loggerFactory;
            Address = namedPipe;
            DisplayName = "IPC";
            Settings = new IpcSettings();
        }

        public IpcSettings Settings { get; }

        public override PolyListener CreateListener()
        {
            Initialize();
            return new IpcListener(this, _protocol, _bufferPool, _loggerFactory);
        }

        public override PolyChannel CreateClient()
        {
            Initialize();
            NamedPipeClientStream clientStream = new NamedPipeClientStream(
                Address.Host, Address.PathAndQuery, PipeDirection.InOut, PipeOptions.Asynchronous);
            return new IpcChannel(clientStream, this, isServer: false, _protocol, _bufferPool, _loggerFactory);
        }

        private void Initialize()
        {
            if (_isInitialized)
                return;
            if (MessageMetadata == null)
                throw new InvalidOperationException("Message metadata needs to be initialized.");

            _protocol = new Protocol(_loggerFactory.CreateLogger(typeof(Protocol)), MessageMetadata, this);
            _bufferPool = ArrayPool<byte>.Create(
                maxArrayLength: MessageBufferSettings.MaxSize,
                maxArraysPerBucket: MessageBufferSettings.MaxArraysPerBucket);
            _isInitialized = true;
        }

        public override IEnumerable<MessageInfo> GetMessageTypes()
        {
            return new [] {new MessageInfo(typeof(ProtocolHeader), ProtocolHeader.TypeID)};
        }

        public override string GetSettingsInfo()
        {
            return "TODO";
        }
    }
}
