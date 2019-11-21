using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PolyMessage.Server
{
    internal interface IAcceptor : IDisposable
    {
        Task Start(PolyTransport transport, PolyFormat format, ServerComponents serverComponents, CancellationToken cancelToken);

        void Stop();

        IEnumerable<IProcessor> GetActiveProcessors();
    }

    internal sealed class Acceptor : IAcceptor
    {
        private PolyListener _listener;
        private readonly ConcurrentDictionary<string, IProcessor> _processors;
        // logging
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        // stop/dispose
        private readonly ManualResetEventSlim _stoppedEvent;
        private bool _isDisposed;
        private bool _isStopRequested;

        public Acceptor(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(GetType());
            _processors = new ConcurrentDictionary<string, IProcessor>();
            _stoppedEvent = new ManualResetEventSlim(initialState: false);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            foreach (KeyValuePair<string, IProcessor> processorKvp in _processors)
            {
                processorKvp.Value.Stop();
            }
            _processors.Clear();
            _listener?.StopAccepting();
            _isStopRequested = true;
            _logger.LogTrace("Waiting worker thread...");
            _stoppedEvent.Wait();

            _listener?.Dispose();
            _stoppedEvent.Dispose();

            _isDisposed = true;
            _logger.LogTrace("Stopped.");
        }

        public async Task Start(PolyTransport transport, PolyFormat format, ServerComponents serverComponents, CancellationToken cancelToken)
        {
            if (_isDisposed)
                throw new InvalidOperationException("Acceptor is already stopped.");

            try
            {
                await DoStart(transport, format, serverComponents, cancelToken).ConfigureAwait(false);
            }
            catch (PolyListenerStoppedException stoppedException)
            {
                _logger.LogInformation("Listener for transport {0} at address {1} has stopped.", stoppedException.Transport.DisplayName, stoppedException.Transport.Address);
            }
            catch (Exception exception)
            {
                // TODO: set exception as the first parameter
                _logger.LogError("Unexpected: {0}", exception);
            }
            finally
            {
                _stoppedEvent.Set();
                _logger.LogTrace("Stopped worker thread.");
            }
        }

        private async Task DoStart(PolyTransport transport, PolyFormat format, ServerComponents serverComponents, CancellationToken cancelToken)
        {
            _listener = transport.CreateListener();
            _listener.PrepareAccepting();

            while (!cancelToken.IsCancellationRequested && !_isStopRequested)
            {
                PolyChannel channel = await _listener.AcceptClient().ConfigureAwait(false);
                channel.Open();
                // TODO: make trace and possibly debug calls with a check
                _logger.LogTrace("Accepted client.");

                IProcessor processor = new Processor(_loggerFactory, format, channel);
                bool added = _processors.TryAdd(processor.ID, processor);
                if (added)
                {
                    Task _ = Task.Run(() => RunProcessor(processor, serverComponents, cancelToken), cancelToken);
                }
                else
                {
                    _logger.LogCritical("Could not add processor with ID {0}.", processor.ID);
                }
            }
        }

        private async Task RunProcessor(IProcessor processor, ServerComponents serverComponents, CancellationToken cancelToken)
        {
            try
            {
                await processor.Start(serverComponents, cancelToken);
            }
            finally
            {
                bool isRemoved = _processors.TryRemove(processor.ID, out _);
                if (!isRemoved)
                    _logger.LogCritical("Failed to remove processor with ID {0}.", processor.ID);
            }
        }

        public void Stop()
        {
            Dispose();
        }

        public IEnumerable<IProcessor> GetActiveProcessors()
        {
            return new List<IProcessor>(_processors.Select(kvp => kvp.Value));
        }
    }
}
