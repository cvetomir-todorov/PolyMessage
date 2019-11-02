using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Endpoints;
using PolyMessage.Formats;
using PolyMessage.Transports;

namespace PolyMessage.Server
{
    internal interface IAcceptor : IDisposable
    {
        Task Start(ITransport transport, IFormat format, IRouter router, CancellationToken cancelToken);

        void Stop();
    }

    internal sealed class DefaultAcceptor : IAcceptor
    {
        private ITransport _transport;
        private readonly HashSet<IProcessor> _processors;
        // logging
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        // stop/dispose
        private readonly ManualResetEventSlim _stoppedEvent;
        private bool _isDisposed;
        private bool _isStopRequested;

        public DefaultAcceptor(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(GetType());
            _processors = new HashSet<IProcessor>();
            _stoppedEvent = new ManualResetEventSlim(initialState: false);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            foreach (IProcessor processor in _processors)
            {
                processor.Stop();
            }
            _transport.StopAccepting();
            _isStopRequested = true;
            _logger.LogTrace("Waiting worker thread...");
            _stoppedEvent.Wait();
            _stoppedEvent.Dispose();

            _isDisposed = true;
            _logger.LogTrace("Stopped.");
        }

        public async Task Start(ITransport transport, IFormat format, IRouter router, CancellationToken cancelToken)
        {
            if (_isDisposed)
                throw new InvalidOperationException("Acceptor is already stopped.");

            try
            {
                await DoStart(transport, format, router, cancelToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError("Unexpected: {0}", exception);
            }
            finally
            {
                _stoppedEvent.Set();
                _logger.LogTrace("Stopped worker thread.");
            }
        }

        private async Task DoStart(ITransport transport, IFormat format, IRouter router, CancellationToken cancelToken)
        {
            _transport = transport;
            await _transport.PrepareAccepting().ConfigureAwait(false);

            IDispatcher dispatcher = new DefaultDispatcher();

            while (!cancelToken.IsCancellationRequested && !_isStopRequested)
            {
                IChannel channel = await _transport.AcceptClient(format).ConfigureAwait(false);
                IProcessor processor = new DefaultProcessor(_loggerFactory);
                // TODO: add stopped event so that we remove the processor when it has finished
                _processors.Add(processor);

                Task _ = processor.Start(channel, router, dispatcher, cancelToken);
            }
        }

        public void Stop()
        {
            Dispose();
        }
    }
}
