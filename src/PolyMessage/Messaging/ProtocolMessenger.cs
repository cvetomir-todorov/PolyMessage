using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Metadata;

namespace PolyMessage.Messaging
{
    internal interface IMessenger
    {
        Task Send(string origin, object message, PolyFormatter formatter, CancellationToken cancelToken);

        Task<object> Receive(string origin, PolyFormatter formatter, CancellationToken cancelToken);
    }

    internal sealed class ProtocolMessenger : IMessenger
    {
        private readonly ILogger _logger;
        private readonly IMessageMetadata _messageMetadata;

        public ProtocolMessenger(ILoggerFactory loggerFactory, IMessageMetadata messageMetadata)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _messageMetadata = messageMetadata;
        }

        public async Task Send(string origin, object message, PolyFormatter formatter, CancellationToken cancelToken)
        {
            PolyHeader header = new PolyHeader();
            header.MessageID = _messageMetadata.GetMessageID(message.GetType());

            _logger.LogTrace("[{0}] Sending header for message with ID {1}...", origin, header.MessageID);
            await formatter.Write(header, cancelToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Sent header for message with ID {1}.", origin, header.MessageID);

            _logger.LogTrace("[{0}] Sending message with ID {1}...", origin, header.MessageID);
            await formatter.Write(message, cancelToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Sent message with ID {1}.", origin, header.MessageID);
        }

        public async Task<object> Receive(string origin, PolyFormatter formatter, CancellationToken cancelToken)
        {
            _logger.LogTrace("[{0}] Receiving header...", origin);
            PolyHeader header = (PolyHeader) await formatter.Read(typeof(PolyHeader), cancelToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Received header for message with ID {1}.", origin, header.MessageID);

            Type messageType = _messageMetadata.GetMessageType(header.MessageID);
            _logger.LogTrace("[{0}] Receiving message with ID {1}...", origin, header.MessageID);
            object message = await formatter.Read(messageType, cancelToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Received message with ID {1}.", origin, header.MessageID);
            // FEAT: validate actual type etc.

            return message;
        }
    }
}
