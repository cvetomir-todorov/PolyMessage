using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Metadata;

namespace PolyMessage.Messaging
{
    internal interface IMessenger
    {
        Task Send(string origin, object message, MessageStream stream, PolyFormatter formatter, CancellationToken ct);

        Task<object> Receive(string origin, MessageStream stream, PolyFormatter formatter, CancellationToken ct);
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

        public async Task Send(string origin, object message, MessageStream stream, PolyFormatter formatter, CancellationToken ct)
        {
            PolyHeader header = new PolyHeader();
            header.MessageTypeID = _messageMetadata.GetMessageTypeID(message.GetType());

            _logger.LogTrace("[{0}] Sending message with type ID {1}...", origin, header.MessageTypeID);

            stream.ResetLengthAndPosition();

            int lengthPrefixPosition = (int) stream.Position;
            stream.ReserveSpaceForLengthPrefix();
            formatter.Serialize(header);
            stream.WriteLengthPrefix(position: lengthPrefixPosition, "header");

            lengthPrefixPosition = (int) stream.Position;
            stream.ReserveSpaceForLengthPrefix();
            formatter.Serialize(message);
            stream.WriteLengthPrefix(position: lengthPrefixPosition, "message");

            await stream.SendToTransport(ct).ConfigureAwait(false);

            _logger.LogTrace("[{0}] Sent message with type ID {1}.", origin, header.MessageTypeID);
        }

        public async Task<object> Receive(string origin, MessageStream stream, PolyFormatter formatter, CancellationToken ct)
        {
            int messageLength = await stream.ReceiveFromTransport("header", ct).ConfigureAwait(false);
            stream.PrepareForDeserialize(messageLength);
            PolyHeader header = (PolyHeader) formatter.Deserialize(typeof(PolyHeader));

            _logger.LogTrace("[{0}] Received header for message with type ID {1}.", origin, header.MessageTypeID);

            messageLength = await stream.ReceiveFromTransport("message", ct).ConfigureAwait(false);
            stream.PrepareForDeserialize(messageLength);
            Type messageType = _messageMetadata.GetMessageType(header.MessageTypeID);
            object message = formatter.Deserialize(messageType);

            _logger.LogTrace("[{0}] Received message with type ID {1}.", origin, header.MessageTypeID);

            return message;
        }
    }
}
