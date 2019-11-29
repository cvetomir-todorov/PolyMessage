using System;
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
        private readonly PolyFormatter _formatter;
        private readonly CancellationToken _cancelToken;
        private readonly IMessageMetadata _messageMetadata;
        private readonly CastToTaskOfResponse _castDelegate;
        private bool _isDisposed;

        public OperationInterceptor(
            ILogger logger,
            string clientID,
            IMessenger messenger,
            PolyFormat format,
            PolyChannel channel,
            CancellationToken cancelToken,
            IMessageMetadata messageMetadata,
            CastToTaskOfResponse castDelegate)
        {
            _logger = logger;
            _clientID = clientID;
            _messenger = messenger;
            _formatter = format.CreateFormatter(channel);
            _cancelToken = cancelToken;
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
            int responseTypeID = _messageMetadata.GetMessageTypeID(responseType);
            Task responseTask = _castDelegate(responseTypeID, responseMessage);
            invocation.ReturnValue = responseTask;
        }

        private async Task<object> CallOperation(object requestMessage)
        {
            _logger.LogTrace("[{0}] Sending request [{1}]...", _clientID, requestMessage);
            await _messenger.Send(_clientID, requestMessage, _formatter, _cancelToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Sent request [{1}] and waiting for response...", _clientID, requestMessage);
            object responseMessage = await _messenger.Receive(_clientID, _formatter, _cancelToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Received response [{1}].", _clientID, responseMessage);

            return responseMessage;
        }
    }
}