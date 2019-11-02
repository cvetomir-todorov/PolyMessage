using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        // logging
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        // stop/dispose
        private readonly CancellationTokenSource _cancelTokenSource;
        private bool _isDisposed;

        public PolyHost(ITransport transport, IFormat format)
            : this(transport, format, new NullLoggerFactory())
        {}

        public PolyHost(ITransport transport, IFormat format, ILoggerFactory loggerFactory)
        {
            // TODO: validate input

            // transport/format
            _transport = transport;
            _format = format;
            // endpoints/contracts
            _endpoints = new List<Endpoint>();
            _contractInspector = new DefaultContractInspector();
            // logging
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(GetType());
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

            Task acceptClientsTask = _acceptor.Start(_transport, _format, router, _cancelTokenSource.Token);
            _logger.LogInformation("Started host with {0} endpoint(s) using {1} transport and {2} format.", _endpoints.Count, _transport.DisplayName, _format.DisplayName);
            return acceptClientsTask;
        }

        public void Stop()
        {
            Dispose();
        }
    }
}
