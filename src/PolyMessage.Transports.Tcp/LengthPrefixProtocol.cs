using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Exceptions;
using PolyMessage.Messaging;
using PolyMessage.Metadata;

namespace PolyMessage.Transports.Tcp
{
    internal sealed class LengthPrefixProtocol
    {
        private readonly ILogger _logger;
        private readonly IReadOnlyMessageMetadata _messageMetadata;
        private readonly PolyTransport _transport;
        // TCP transport uses only 1 stream
        private const string TcpStreamID = "0";

        public LengthPrefixProtocol(ILogger logger, IReadOnlyMessageMetadata messageMetadata, PolyTransport transport)
        {
            _logger = logger;
            _messageMetadata = messageMetadata;
            _transport = transport;
        }

        public async Task Send(object message, PolyFormatter formatter, LengthPrefixStream stream, string origin, CancellationToken ct)
        {
            PolyHeader header = new PolyHeader();
            header.MessageTypeID = _messageMetadata.GetMessageTypeID(message.GetType());

            _logger.LogTrace("[{0}] Sending message with type ID {1}...", origin, header.MessageTypeID);

            stream.Origin = origin;
            stream.ResetLengthAndPosition();

            int lengthPrefixPosition = (int)stream.Position;
            stream.ReserveSpaceForLengthPrefix();
            formatter.Serialize(header, TcpStreamID, stream);
            stream.WriteLengthPrefix(position: lengthPrefixPosition, "header");

            lengthPrefixPosition = (int)stream.Position;
            stream.ReserveSpaceForLengthPrefix();
            formatter.Serialize(message, TcpStreamID, stream);
            stream.WriteLengthPrefix(position: lengthPrefixPosition, "message");

            await stream.SendToTransport(ct).ConfigureAwait(false);

            _logger.LogTrace("[{0}] Sent message with type ID {1}.", origin, header.MessageTypeID);
        }

        public async Task<object> Receive(PolyFormatter formatter, LengthPrefixStream stream, string origin, CancellationToken ct)
        {
            stream.Origin = origin;

            int messageLength = await stream.ReceiveFromTransport("header", ct).ConfigureAwait(false);
            if (messageLength < 0)
            {
                throw new PolyConnectionClosedException(PolyConnectionCloseReason.ConnectionClosed, _transport);
            }
            stream.PrepareForDeserialize(messageLength);
            PolyHeader header = (PolyHeader)formatter.Deserialize(typeof(PolyHeader), TcpStreamID, stream);

            _logger.LogTrace("[{0}] Received header for message with type ID {1}.", origin, header.MessageTypeID);

            messageLength = await stream.ReceiveFromTransport("message", ct).ConfigureAwait(false);
            if (messageLength < 0)
            {
                throw new PolyConnectionClosedException(PolyConnectionCloseReason.ConnectionClosed, _transport);
            }
            stream.PrepareForDeserialize(messageLength);
            Type messageType = _messageMetadata.GetMessageType(header.MessageTypeID);
            object message = formatter.Deserialize(messageType, TcpStreamID, stream);

            _logger.LogTrace("[{0}] Received message with type ID {1}.", origin, header.MessageTypeID);

            return message;
        }
    }
}
