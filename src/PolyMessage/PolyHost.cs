using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyMessage.Contracts;
using PolyMessage.Endpoints;
using PolyMessage.Formats;
using PolyMessage.Server;
using PolyMessage.Transports;

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
            // endpoints/contracts
            _endpoints = new List<Endpoint>();
            _contractInspector = new DefaultContractInspector();
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

            _acceptor?.Dispose();
            _transport.Dispose();

            _isDisposed = true;
            _logger.LogInformation("Stopped.");
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException("Host is already stopped.");
        }

        public void AddContract<TContract, TImplementation>()
            where TImplementation : class, TContract
        {
            EnsureNotDisposed();

            IEnumerable<Endpoint> endpoints = _contractInspector.InspectContract(typeof(TContract), typeof(TImplementation));
            _endpoints.AddRange(endpoints);
        }

        public Task Start()
        {
            EnsureNotDisposed();
            if (_endpoints.Count <= 0)
                throw new InvalidOperationException("No contracts added or none of them have endpoints.");

            _acceptor = new DefaultAcceptor(_loggerFactory);
            IRouter router = new DefaultRouter(_endpoints);
            IDispatcher dispatcher = new DefaultDispatcher(_serviceProvider);

            Task acceptClientsTask = Task.Run(async () => await _acceptor.Start(_transport, _format, router, dispatcher, _cancelTokenSource.Token));
            _logger.LogInformation("Started host with {0} endpoint(s) using {1} transport and {2} format.", _endpoints.Count, _transport.DisplayName, _format.DisplayName);
            return acceptClientsTask;
        }

        public void Stop()
        {
            Dispose();
        }
    }
}
