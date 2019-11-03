using System;
using System.Collections.Generic;
using System.Threading;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PolyMessage.Proxies;

namespace PolyMessage
{
    public class PolyClient : IDisposable
    {
        // transport/format
        private readonly ITransport _transport;
        private readonly IFormat _format;
        // proxies
        private readonly Dictionary<Type, object> _proxies;
        private readonly IProxyGenerator _proxyGenerator;
        private IChannel _channel;
        private static readonly object _createChannelLock = new object();
        // logging
        private readonly ILogger _logger;
        // identity
        private static int _generation;
        private readonly string _id;
        // stop/dispose
        private readonly CancellationTokenSource _cancelTokenSource;
        private bool _isDisposed;

        public PolyClient(ITransport transport, IFormat format)
            : this(transport, format, new NullLoggerFactory())
        {}

        public PolyClient(ITransport transport, IFormat format, ILoggerFactory loggerFactory)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (format == null)
                throw new ArgumentNullException(nameof(format));
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            // transport/format
            _transport = transport;
            _format = format;
            // proxies
            _proxies = new Dictionary<Type, object>();
            _proxyGenerator = new ProxyGenerator();
            // logging
            _logger = loggerFactory.CreateLogger(GetType());
            _id = "Client" + Interlocked.Increment(ref _generation);
            // stop/dispose
            _cancelTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _cancelTokenSource.Cancel();
            _cancelTokenSource.Dispose();

            _isDisposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException("Client is already disposed.");
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
            EnsureChannelCreated();
            IInterceptor endpointInterceptor = new EndpointInterceptor(_logger, _id, _channel, _cancelTokenSource.Token);
            object proxy = _proxyGenerator.CreateInterfaceProxyWithoutTarget(contractType, new Type[0], endpointInterceptor);
            return proxy;
        }

        private void EnsureChannelCreated()
        {
            if (_channel == null)
            {
                lock (_createChannelLock)
                {
                    if (_channel == null)
                    {
                        _channel = _transport.CreateClient(_format);
                    }
                }
            }
        }
    }
}
