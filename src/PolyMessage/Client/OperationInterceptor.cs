using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using PolyMessage.CodeGeneration;
using PolyMessage.Metadata;

namespace PolyMessage.Client
{
    internal sealed class OperationInterceptor : IInterceptor, IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _clientID;
        private readonly PolyChannel _channel;
        private readonly PolyFormatter _formatter;
        private readonly CancellationToken _cancellationToken;
        private readonly IMessageMetadata _messageMetadata;
        private readonly CastToTaskOfResponse _castDelegate;
        private bool _isDisposed;

        public OperationInterceptor(
            ILoggerFactory loggerFactory,
            string clientID,
            PolyFormat format,
            PolyChannel channel,
            CancellationToken cancellationToken,
            IMessageMetadata messageMetadata,
            CastToTaskOfResponse castDelegate)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _clientID = clientID;
            _channel = channel;
            _formatter = format.CreateFormatter();
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
            _logger.LogTrace("[{0}] Sending request [{1}]...", _clientID, requestMessage.GetType());
            await _channel.Send(requestMessage, _formatter, _clientID, _cancellationToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Sent request [{1}] and waiting for response...", _clientID, requestMessage.GetType());
            object responseMessage = await _channel.Receive(_formatter , _clientID, _cancellationToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Received response [{1}].", _clientID, responseMessage.GetType());

            return responseMessage;
        }
    }
}