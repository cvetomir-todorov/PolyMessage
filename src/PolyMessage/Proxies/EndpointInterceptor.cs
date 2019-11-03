using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;

namespace PolyMessage.Proxies
{
    internal class EndpointInterceptor : IInterceptor
    {
        private readonly ILogger _logger;
        private readonly IChannel _channel;
        private readonly string _clientID;
        private readonly CancellationToken _cancelToken;

        public EndpointInterceptor(ILogger logger, string clientID, IChannel channel, CancellationToken cancelToken)
        {
            _logger = logger;
            _clientID = clientID;
            _channel = channel;
            _cancelToken = cancelToken;
        }

        public void Intercept(IInvocation invocation)
        {
            string requestMessage = (string) invocation.Arguments[0];
            invocation.ReturnValue = CallEndpoint(requestMessage);
        }

        private async Task<string> CallEndpoint(string requestMessage)
        {
            _logger.LogTrace("[{0}] Sending request [{1}]...", _clientID, requestMessage);
            await _channel.Send(requestMessage, _cancelToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Sent request [{1}] and waiting for response...", _clientID, requestMessage);
            string responseMessage = await _channel.Receive(_cancelToken).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Received response [{1}].", _clientID, responseMessage);

            return responseMessage;
        }
    }
}