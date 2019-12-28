using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Metadata;

namespace PolyMessage.Messaging
{
    internal interface IMessenger
    {
        Task Send(string origin, object message, MessagingStream stream, PolyFormatter formatter, CancellationToken ct);

        Task<object> Receive(string origin, MessagingStream stream, PolyFormatter formatter, CancellationToken ct);
    }

    internal sealed class Messenger : IMessenger
    {
        private readonly ILogger _logger;
        private readonly IMessageMetadata _messageMetadata;

        public Messenger(ILoggerFactory loggerFactory, IMessageMetadata messageMetadata)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _messageMetadata = messageMetadata;
        }

        public async Task Send(string origin, object message, MessagingStream stream, PolyFormatter formatter, CancellationToken ct)
        {
            PolyHeader header = new PolyHeader();
            header.MessageTypeID = _messageMetadata.GetMessageTypeID(message.GetType());

            _logger.LogTrace("[{0}] Sending header for message with type ID {1}...", origin, header.MessageTypeID);
            await SendObject(header, stream, formatter, ct).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Sent header for message with type ID {1}.", origin, header.MessageTypeID);

            _logger.LogTrace("[{0}] Sending message with type ID {1}...", origin, header.MessageTypeID);
            await SendObject(message, stream, formatter, ct).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Sent message with type ID {1}.", origin, header.MessageTypeID);
        }

        private async Task SendObject(object message, MessagingStream stream, PolyFormatter formatter, CancellationToken ct)
        {
            stream.Reset();
            formatter.Serialize(message);
            await stream.WriteMessageToTransport(ct).ConfigureAwait(false);
        }

        public async Task<object> Receive(string origin, MessagingStream stream, PolyFormatter formatter, CancellationToken ct)
        {
            _logger.LogTrace("[{0}] Receiving header...", origin);
            PolyHeader header = (PolyHeader) await ReceiveObject(typeof(PolyHeader), stream, formatter, ct).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Received header for message with type ID {1}.", origin, header.MessageTypeID);

            Type messageType = _messageMetadata.GetMessageType(header.MessageTypeID);
            _logger.LogTrace("[{0}] Receiving message with type ID {1}...", origin, header.MessageTypeID);
            object message = await ReceiveObject(messageType, stream, formatter, ct).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Received message with type ID {1}.", origin, header.MessageTypeID);

            return message;
        }

        private async Task<object> ReceiveObject(Type objType, MessagingStream stream, PolyFormatter formatter, CancellationToken ct)
        {
            await stream.ReadMessageFromTransport(ct).ConfigureAwait(false);
            return formatter.Deserialize(objType);
        }
    }
}
