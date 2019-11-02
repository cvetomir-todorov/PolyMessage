using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PolyMessage.Formats;
using PolyMessage.Transports;

namespace PolyMessage
{
    public class PolyProxy : IDisposable
    {
        // transport/format
        private readonly ITransport _transport;
        private readonly IFormat _format;
        // proxies
        private readonly Dictionary<Type, object> _proxies;
        private readonly IProxyGenerator _proxyGenerator;
        // logging
        private readonly ILogger _logger;
        // identity
        private static int _generation;
        private readonly string _id;
        // stop/dispose
        private readonly CancellationTokenSource _cancelTokenSource;
        private bool _isDisposed;

        public PolyProxy(ITransport transport, IFormat format)
            : this(transport, format, new NullLoggerFactory())
        {}

        public PolyProxy(ITransport transport, IFormat format, ILoggerFactory loggerFactory)
        {
            // TODO: validate input

            // transport/format
            _transport = transport;
            _format = format;
            // proxies
            _proxies = new Dictionary<Type, object>();
            _proxyGenerator = new ProxyGenerator();
            // logging
            _logger = loggerFactory.CreateLogger(GetType());
            _id = "Proxy" + Interlocked.Increment(ref _generation);
            // stop/dispose
            _cancelTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _cancelTokenSource.Cancel();
            _cancelTokenSource.Dispose();
            _transport.Dispose();

            _isDisposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException("Proxy is already disposed.");
        }

        public void AddContract<TContract>()
            where TContract : class
        {
            EnsureNotDisposed();

            Type contractType = typeof(TContract);
            object proxy = CreateProxy(contractType);
            _proxies.Add(contractType, proxy);
        }

        public TContract Get<TContract>()
            where TContract : class
        {
            EnsureNotDisposed();

            if (_proxies.TryGetValue(typeof(TContract), out object proxy))
            {
                TContract contract = (TContract) proxy;
                return contract;
            }
            else
            {
                throw new InvalidOperationException($"Contract {typeof(TContract).FullName} needs to be added first.");
            }
        }

        private object CreateProxy(Type contractType)
        {
            // TODO: figure out how to create the channel w/o connecting to the server
            // TODO: reuse the channel for all different dynamic proxy instances
            IChannel channel = _transport.CreateClient(_format);
            IInterceptor endpointInterceptor = new EndpointInterceptor(_logger, _id, channel, _cancelTokenSource.Token);

            object proxy = _proxyGenerator.CreateInterfaceProxyWithoutTarget(
                contractType, new Type[0], endpointInterceptor);
            return proxy;
        }

        private class EndpointInterceptor : IInterceptor
        {
            private readonly ILogger _logger;
            private readonly IChannel _channel;
            private readonly string _proxyID;
            private readonly CancellationToken _cancelToken;

            public EndpointInterceptor(ILogger logger, string proxyID, IChannel channel, CancellationToken cancelToken)
            {
                _logger = logger;
                _proxyID = proxyID;
                _channel = channel;
                _cancelToken = cancelToken;
            }

            public void Intercept(IInvocation invocation)
            {
                string requestMessage = (string) invocation.Arguments[0];

                // TODO: do we need to call Task.Run or can we combine the tasks somehow?
                Task<string> sendReceiveTask = Task.Run(async () =>
                {
                    _logger.LogTrace("[{0}] Sending request [{1}]...", _proxyID, requestMessage);
                    await _channel.Send(requestMessage, _cancelToken).ConfigureAwait(false);
                    _logger.LogTrace("[{0}] Sent request [{1}] and waiting for response...", _proxyID, requestMessage);
                    string responseMessage = await _channel.Receive(_cancelToken).ConfigureAwait(false);
                    _logger.LogTrace("[{0}] Received response [{1}].", _proxyID, responseMessage);

                    return responseMessage;
                }, _cancelToken);

                invocation.ReturnValue = sendReceiveTask;
            }
        }
    }
}
