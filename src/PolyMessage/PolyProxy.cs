using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using PolyMessage.Formats;
using PolyMessage.Transports;

namespace PolyMessage
{
    public class PolyProxy : IDisposable
    {
        // outside
        private readonly ITransport _transport;
        private readonly IFormat _format;
        // proxies
        private readonly Dictionary<Type, object> _proxies;
        private readonly IProxyGenerator _proxyGenerator;
        // inside fields
        private readonly CancellationTokenSource _cancelTokenSource;
        private bool _isDisposed;

        public PolyProxy(ITransport transport, IFormat format)
        {
            // TODO: validate input

            // outside
            _transport = transport;
            _format = format;
            // proxies
            _proxies = new Dictionary<Type, object>();
            _proxyGenerator = new ProxyGenerator();
            // other
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
            IChannel channel = _transport.CreateClient(_format);
            IInterceptor endpointInterceptor = new EndpointInterceptor(channel, _cancelTokenSource.Token);

            object proxy = _proxyGenerator.CreateInterfaceProxyWithoutTarget(
                contractType, new Type[0], endpointInterceptor);
            return proxy;
        }

        private class EndpointInterceptor : IInterceptor
        {
            private readonly IChannel _channel;
            private readonly CancellationToken _cancelToken;

            public EndpointInterceptor(IChannel channel, CancellationToken cancelToken)
            {
                _channel = channel;
                _cancelToken = cancelToken;
            }

            public void Intercept(IInvocation invocation)
            {
                string requestMessage = (string) invocation.Arguments[0];

                Task<string> sendReceiveTask = Task.Run(async () =>
                {
                    await _channel.Send(requestMessage, _cancelToken).ConfigureAwait(false);
                    string responseMessage = await _channel.Receive(_cancelToken).ConfigureAwait(false);
                    return responseMessage;
                }, _cancelToken);

                //_channel.Send(requestMessage, _cancelToken);
                //Task<string> receiveTask = _channel.Receive(_cancelToken);

                invocation.ReturnValue = sendReceiveTask;
            }
        }
    }
}
