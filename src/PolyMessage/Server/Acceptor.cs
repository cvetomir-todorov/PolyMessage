using System;
using System.Buffers;
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
        Task Start(PolyTransport transport, PolyFormat format, ServerComponents serverComponents, CancellationToken ct);

        void Stop();

        IEnumerable<IProcessor> GetActiveProcessors();
    }

    internal sealed class Acceptor : IAcceptor
    {
        private PolyListener _listener;
        private readonly ArrayPool<byte> _bufferPool;
        private readonly ConcurrentDictionary<string, IProcessor> _processors;
        // .net core integration
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        // stop/dispose
        private readonly ManualResetEventSlim _stoppedEvent;
        private bool _isDisposed;
        private bool _isStopRequested;

        public Acceptor(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, ArrayPool<byte> bufferPool)
        {
            _serviceProvider = serviceProvider;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(GetType());
            _bufferPool = bufferPool;
            _processors = new ConcurrentDictionary<string, IProcessor>();
            _stoppedEvent = new ManualResetEventSlim(initialState: true);
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
            _logger.LogTrace("Waiting for worker thread...");
            _stoppedEvent.Wait();

            _listener?.Dispose();
            _stoppedEvent.Dispose();

            _isDisposed = true;
            _logger.LogTrace("Stopped.");
        }

        public async Task Start(PolyTransport transport, PolyFormat format, ServerComponents serverComponents, CancellationToken ct)
        {
            if (_isDisposed)
                throw new InvalidOperationException("Acceptor is already stopped.");

            try
            {
                _stoppedEvent.Reset();
                await DoStart(transport, format, serverComponents, ct).ConfigureAwait(false);
            }
            catch (PolyListenerStoppedException stoppedException)
            {
                _logger.LogInformation("Listener for transport {0} at address {1} has stopped.", stoppedException.Transport.DisplayName, stoppedException.Transport.Address);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error occurred.");
            }
            finally
            {
                _stoppedEvent.Set();
                _logger.LogTrace("Stopped worker thread.");
            }
        }

        private async Task DoStart(PolyTransport transport, PolyFormat format, ServerComponents serverComponents, CancellationToken ct)
        {
            _listener = transport.CreateListener();
            _listener.PrepareAccepting();

            while (!ct.IsCancellationRequested && !_isStopRequested)
            {
                Func<PolyChannel> createClient = await _listener.AcceptClient().ConfigureAwait(false);
                Task _ = Task.Run(() => ProcessClient(format, createClient, serverComponents, ct), ct);
            }
        }

        private async Task ProcessClient(PolyFormat format, Func<PolyChannel> createClient, ServerComponents serverComponents, CancellationToken ct)
        {
            IProcessor processor = null;
            try
            {
                PolyChannel client = createClient();
                processor = new Processor(_serviceProvider, _loggerFactory, _bufferPool, format, client);
                if (!_processors.TryAdd(processor.ID, processor))
                    _logger.LogCritical("Failed add processor with ID {0} to list of tracked processors.", processor.ID);

                await processor.Start(serverComponents, ct);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failed to start processor.");
            }
            finally
            {
                if (processor != null)
                {
                    processor.Stop();
                    bool isRemoved = _processors.TryRemove(processor.ID, out _);
                    if (!isRemoved)
                        _logger.LogCritical("Failed to remove processor with ID {0} from list of tracked processors.", processor.ID);
                }
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
