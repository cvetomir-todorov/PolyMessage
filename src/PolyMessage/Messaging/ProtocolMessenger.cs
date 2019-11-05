using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Metadata;

namespace PolyMessage.Messaging
{
    internal interface IMessenger
    {
        Task Send(object message, IFormat format, IChannel channel, CancellationToken cancelToken);

        Task<object> Receive(IFormat format, IChannel channel, CancellationToken cancelToken);
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

        public async Task Send(object message, IFormat format, IChannel channel, CancellationToken cancelToken)
        {
            PolyHeader header = new PolyHeader();
            header.MessageID = _messageMetadata.GetMessageID(message.GetType());

            _logger.LogTrace("Sending header for message with ID {0}...", header.MessageID);
            await format.Write(header, channel, cancelToken).ConfigureAwait(false);
            _logger.LogTrace("Sent header for message with ID {0}.", header.MessageID);

            _logger.LogTrace("Sending message with ID {0}...", header.MessageID);
            await format.Write(message, channel, cancelToken).ConfigureAwait(false);
            _logger.LogTrace("Sent message with ID {0}.", header.MessageID);
        }

        public async Task<object> Receive(IFormat format, IChannel channel, CancellationToken cancelToken)
        {
            _logger.LogTrace("Receiving header...");
            PolyHeader header = (PolyHeader) await format.Read(typeof(PolyHeader), channel, cancelToken).ConfigureAwait(false);
            _logger.LogTrace("Received header for message with ID {0}.", header.MessageID);

            Type messageType = _messageMetadata.GetMessageType(header.MessageID);
            _logger.LogTrace("Receiving message with ID {0}...", header.MessageID);
            object message = await format.Read(messageType, channel, cancelToken).ConfigureAwait(false);
            _logger.LogTrace("Received message with ID {0}.", header.MessageID);
            // FEAT: validate actual type etc.

            return message;
        }

        [Serializable]
        private sealed class PolyHeader
        {
            public int MessageID { get; set; }
        }
    }
}
