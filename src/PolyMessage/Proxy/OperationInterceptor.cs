using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using PolyMessage.CodeGeneration;
using PolyMessage.Messaging;
using PolyMessage.Metadata;

namespace PolyMessage.Proxy
{
    internal sealed class OperationInterceptor : IInterceptor, IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _clientID;
        private readonly IMessenger _messenger;
        private readonly MessagingStream _messagingStream;
        private readonly PolyFormatter _formatter;
        private readonly CancellationToken _cancellationToken;
        private readonly IMessageMetadata _messageMetadata;
        private readonly CastToTaskOfResponse _castDelegate;
        private bool _isDisposed;

        public OperationInterceptor(
            ILoggerFactory loggerFactory,
            string clientID,
            IMessenger messenger,
            PolyFormat format,
            PolyChannel channel,
            CancellationToken cancellationToken,
            IMessageMetadata messageMetadata,
            CastToTaskOfResponse castDelegate)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _clientID = clientID;
            _messenger = messenger;
            // TODO: set array pool and capacity
            _messagingStream = new MessagingStream(_clientID, channel, ArrayPool<byte>.Shared, capacity: 1024, loggerFactory);
            _formatter = format.CreateFormatter(_messagingStream);
            _cancellationToken = cancellationToken;
            _messageMetadata = messageMetadata;
            _castDelegate = castDelegate;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _formatter?.Dispose();
            _isDisposed = true;
        }

        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.IsSpecialName ||
                invocation.Arguments.Length != 1 ||
                invocation.Method.ReturnType.BaseType != typeof(Task))
            {
                invocation.Proceed();
                return;
            }

            object requestMessage = invocation.Arguments[0];
            Task<object> responseMessage = CallOperation(requestMessage);

            // the Task<> is not covariant so we cannot cast Task<object> reference to Task<Response>
            // so we do it manually but with generated code in order to avoid reflection at runtime
            Type responseType = invocation.Method.ReturnType.GenericTypeArguments[0];
            short responseTypeID = _messageMetadata.GetMessageTypeID(responseType);
            Task responseTask = _castDelegate(responseTypeID, responseMessage);
            invocation.ReturnValue = responseTask;
        }

        private async Task<object> CallOperation(object requestMessage)
        {
            _logger.LogTrace("[{0}] Sending request [{1}]...", _clientID, requestMessage);
            await _messenger.Send(_clientID, requestMessage, _messagingStream, _formatter, _cancellationToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Sent request [{1}] and waiting for response...", _clientID, requestMessage);
            object responseMessage = await _messenger.Receive(_clientID, _messagingStream, _formatter, _cancellationToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Received response [{1}].", _clientID, responseMessage);

            return responseMessage;
        }
    }
}