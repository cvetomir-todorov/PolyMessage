using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Metadata;

// TODO: fix all cancelToken variables

namespace PolyMessage.Messaging
{
    internal interface IMessenger
    {
        Task Send(string origin, object message, MessagingStream stream, PolyFormatter formatter, CancellationToken cancellationToken);

        Task<object> Receive(string origin, MessagingStream stream, PolyFormatter formatter, CancellationToken cancellationToken);
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

        public async Task Send(string origin, object message, MessagingStream stream, PolyFormatter formatter, CancellationToken cancellationToken)
        {
            PolyHeader header = new PolyHeader();
            header.MessageTypeID = _messageMetadata.GetMessageTypeID(message.GetType());

            _logger.LogTrace("[{0}] Sending header for message with type ID {1}...", origin, header.MessageTypeID);
            await SendObject(origin, header, stream, formatter, cancellationToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Sent header for message with type ID {1}.", origin, header.MessageTypeID);

            _logger.LogTrace("[{0}] Sending message with type ID {1}...", origin, header.MessageTypeID);
            await SendObject(origin, message, stream, formatter, cancellationToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Sent message with type ID {1}.", origin, header.MessageTypeID);
        }

        private async Task SendObject(string origin, object message, MessagingStream stream, PolyFormatter formatter, CancellationToken cancellationToken)
        {
            stream.Reset();
            formatter.Serialize(message);
            await stream.WriteMessageToTransport(origin, cancellationToken).ConfigureAwait(false);
        }

        public async Task<object> Receive(string origin, MessagingStream stream, PolyFormatter formatter, CancellationToken cancellationToken)
        {
            _logger.LogTrace("[{0}] Receiving header...", origin);
            PolyHeader header = (PolyHeader) await ReceiveObject(origin, typeof(PolyHeader), stream, formatter, cancellationToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Received header for message with type ID {1}.", origin, header.MessageTypeID);

            Type messageType = _messageMetadata.GetMessageType(header.MessageTypeID);
            _logger.LogTrace("[{0}] Receiving message with type ID {1}...", origin, header.MessageTypeID);
            object message = await ReceiveObject(origin, messageType, stream, formatter, cancellationToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Received message with type ID {1}.", origin, header.MessageTypeID);

            return message;
        }

        private async Task<object> ReceiveObject(string origin, Type objType, MessagingStream stream, PolyFormatter formatter, CancellationToken cancellationToken)
        {
            await stream.ReadMessageFromTransport(origin, cancellationToken).ConfigureAwait(false);
            return formatter.Deserialize(objType);
        }
    }
}
