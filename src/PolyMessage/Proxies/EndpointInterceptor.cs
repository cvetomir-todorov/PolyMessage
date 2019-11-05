using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using PolyMessage.Messaging;

namespace PolyMessage.Proxies
{
    internal sealed class EndpointInterceptor : IInterceptor
    {
        private readonly ILogger _logger;
        private readonly string _clientID;
        private readonly IMessenger _messenger;
        private readonly IFormat _format;
        private readonly IChannel _channel;
        private readonly CancellationToken _cancelToken;
        private readonly MethodInfo _castMethod;

        public EndpointInterceptor(
            ILogger logger,
            string clientID,
            IMessenger messenger,
            IFormat format,
            IChannel channel,
            CancellationToken cancelToken)
        {
            _logger = logger;
            _clientID = clientID;
            _messenger = messenger;
            _format = format;
            _channel = channel;
            _cancelToken = cancelToken;
            _castMethod = GetType().GetMethod(nameof(Cast), BindingFlags.Static | BindingFlags.NonPublic);
        }

        public void Intercept(IInvocation invocation)
        {
            object requestMessage = invocation.Arguments[0];
            Task<object> responseMessage = CallEndpoint(requestMessage);

            // TODO: reuse this casting (and the same in the dispatcher) and make it faster
            // get the response type inside of the task: when returning Task<T> we want to get T
            Type responseType = invocation.Method.ReturnType.GenericTypeArguments[0];
            // we will cast the Task<object> to Task<T> where T is the response type
            MethodInfo specificMethod = _castMethod.MakeGenericMethod(responseType);
            object taskOfResponseType = specificMethod.Invoke(null, new object[] {responseMessage});

            invocation.ReturnValue = taskOfResponseType;
        }

        private async Task<object> CallEndpoint(object requestMessage)
        {
            _logger.LogTrace("[{0}] Sending request [{1}]...", _clientID, requestMessage);
            await _messenger.Send(requestMessage, _format, _channel, _cancelToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Sent request [{1}] and waiting for response...", _clientID, requestMessage);
            object responseMessage = await _messenger.Receive(_format, _channel, _cancelToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Received response [{1}].", _clientID, responseMessage);

            return responseMessage;
        }

        private static async Task<TDestination> Cast<TDestination>(Task<object> sourceTask)
        {
            TDestination destination = (TDestination)await sourceTask.ConfigureAwait(false);
            return destination;
        }
    }
}