using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyMessage.Messaging;
using PolyMessage.Metadata;
using PolyMessage.Server;

namespace PolyMessage
{
    /// <summary>
    /// Creates a host for communicating via a certain <see cref="ITransport"/>  with messages in a certain <see cref="IFormat"/>.
    /// </summary>
    public sealed class PolyHost : IDisposable
    {
        // transport/format
        private readonly ITransport _transport;
        private readonly IFormat _format;
        // contracts
        private readonly List<Operation> _operations;
        private readonly IContractInspector _contractInspector;
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
            // contracts
            _operations = new List<Operation>();
            _contractInspector = new ContractInspector();
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

            IEnumerable<Operation> operations = _contractInspector.InspectContract(typeof(TContract));
            _operations.AddRange(operations);
        }

        public void Start()
        {
            EnsureNotDisposed();
            if (_operations.Count <= 0)
                throw new InvalidOperationException("No contracts added or none of them have operations.");

            _acceptor = new Acceptor(_loggerFactory);
            IMessageMetadata messageMetadata = new MessageMetadata();
            IRouter router = new Router();
            IMessenger messenger = new ProtocolMessenger(_loggerFactory, messageMetadata);
            IDispatcher dispatcher = new Dispatcher(_serviceProvider);

            messageMetadata.Build(_operations);
            router.BuildRoutingTable(_operations);

            ServerComponents serverComponents = new ServerComponents(router, messageMetadata, messenger, dispatcher);

            Task _ = Task.Run(async () => await _acceptor.Start(_transport, _format, serverComponents, _cancelTokenSource.Token));
            _logger.LogInformation(
                "Started host using {0} transport listening at {1} and {2} format with {3} operation(s).",
                _transport.DisplayName, _transport.Address, _format.DisplayName, _operations.Count);
        }

        public void Stop()
        {
            Dispose();
        }
    }
}
