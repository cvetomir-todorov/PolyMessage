using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyMessage.Endpoints;
using PolyMessage.Messaging;
using PolyMessage.Server;

namespace PolyMessage
{
    /// <summary>
    /// Creates a host for communicating via a certain <see cref="ITransport"/>
    /// with messages in a certain <see cref="IFormat"/>
    /// using a number of endpoint contracts.
    /// </summary>
    public sealed class PolyHost : IDisposable
    {
        // transport/format
        private readonly ITransport _transport;
        private readonly IFormat _format;
        // endpoints/contracts
        private readonly List<Endpoint> _endpoints;
        private readonly IEndpointBuilder _endpointBuilder;
        private IAcceptor _acceptor;
        // messaging
        private readonly IServiceProvider _serviceProvider;
        // logging
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        // stop/dispose
        private readonly CancellationTokenSource _cancelTokenSource;
        private bool _isDisposed;

        public PolyHost(ITransport transport, IFormat format, IServiceProvider serviceProvider)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (format == null)
                throw new ArgumentNullException(nameof(format));
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            // transport/format
            _transport = transport;
            _format = format;
            // endpoints/contracts
            _endpoints = new List<Endpoint>();
            _endpointBuilder = new DefaultEndpointBuilder();
            // messaging
            _serviceProvider = serviceProvider;
            // logging
            _loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = _loggerFactory.CreateLogger(GetType());
            // stop/dispose
            _cancelTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
         {
            if (_isDisposed)
                return;

            _cancelTokenSource.Cancel();
            _acceptor?.Stop();

            _cancelTokenSource.Dispose();
            _acceptor?.Dispose();

            _isDisposed = true;
            _logger.LogInformation("Stopped.");
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException("Host is already stopped.");
        }

        public void AddContract<TContract>() where TContract : class
        {
            EnsureNotDisposed();

            IEnumerable<Endpoint> endpoints = _endpointBuilder.InspectContract(typeof(TContract));
            _endpoints.AddRange(endpoints);
        }

        public void Start()
        {
            EnsureNotDisposed();
            if (_endpoints.Count <= 0)
                throw new InvalidOperationException("No contracts added or none of them have endpoints.");

            _acceptor = new DefaultAcceptor(_loggerFactory);
            IMessageMetadata messageMetadata = new DefaultMessageMetadata();
            IRouter router = new DefaultRouter();
            IMessenger messenger = new ProtocolMessenger(_loggerFactory, messageMetadata);
            IDispatcher dispatcher = new DefaultDispatcher(_serviceProvider);

            messageMetadata.Build(_endpoints);
            router.BuildRoutingTable(_endpoints);

            ServerComponents serverComponents = new ServerComponents(router, messageMetadata, messenger, dispatcher);

            Task _ = Task.Run(async () => await _acceptor.Start(_transport, _format, serverComponents, _cancelTokenSource.Token));
            _logger.LogInformation(
                "Started host using {0} transport listening at {1} and {2} format with {3} endpoint(s).",
                _transport.DisplayName, _transport.Address, _format.DisplayName, _endpoints.Count);
        }

        public void Stop()
        {
            Dispose();
        }
    }
}
