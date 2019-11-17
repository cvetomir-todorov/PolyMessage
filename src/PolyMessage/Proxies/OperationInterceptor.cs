﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using PolyMessage.CodeGeneration;
using PolyMessage.Messaging;
using PolyMessage.Metadata;

namespace PolyMessage.Proxies
{
    internal sealed class OperationInterceptor : IInterceptor
    {
        private readonly ILogger _logger;
        private readonly string _clientID;
        private readonly IMessenger _messenger;
        private readonly IFormat _format;
        private readonly IChannel _channel;
        private readonly CancellationToken _cancelToken;
        private readonly IMessageMetadata _messageMetadata;
        private readonly CastTaskOfObjectToTaskOfResponse _castDelegate;

        public OperationInterceptor(
            ILogger logger,
            string clientID,
            IMessenger messenger,
            IFormat format,
            IChannel channel,
            CancellationToken cancelToken,
            IMessageMetadata messageMetadata,
            CastTaskOfObjectToTaskOfResponse castDelegate)
        {
            _logger = logger;
            _clientID = clientID;
            _messenger = messenger;
            _format = format;
            _channel = channel;
            _cancelToken = cancelToken;
            _messageMetadata = messageMetadata;
            _castDelegate = castDelegate;
        }

        public void Intercept(IInvocation invocation)
        {
            object requestMessage = invocation.Arguments[0];
            Task<object> responseMessage = CallOperation(requestMessage);

            Type responseType = invocation.Method.ReturnType.GenericTypeArguments[0];
            int responseID = _messageMetadata.GetMessageID(responseType);
            Task responseTask = _castDelegate(responseID, responseMessage);
            invocation.ReturnValue = responseTask;
        }

        private async Task<object> CallOperation(object requestMessage)
        {
            _logger.LogTrace("[{0}] Sending request [{1}]...", _clientID, requestMessage);
            await _messenger.Send(requestMessage, _format, _channel, _cancelToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Sent request [{1}] and waiting for response...", _clientID, requestMessage);
            object responseMessage = await _messenger.Receive(_format, _channel, _cancelToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Received response [{1}].", _clientID, responseMessage);

            return responseMessage;
        }
    }
}