using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Exceptions;

namespace PolyMessage.Server
{
    internal interface IAcceptor : IDisposable
    {
        Task Start(PolyTransport transport, PolyFormat format, ServerComponents serverComponents, CancellationToken ct);

        void Stop();

        IEnumerable<ISession> GetSessions();
    }

    internal sealed class Acceptor : IAcceptor
    {
        private PolyListener _listener;
        private readonly ArrayPool<byte> _bufferPool;
        private readonly ConcurrentDictionary<string, ISession> _sessions;
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
            _sessions = new ConcurrentDictionary<string, ISession>();
            _stoppedEvent = new ManualResetEventSlim(initialState: true);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _listener?.StopAccepting();
            _isStopRequested = true;
            _logger.LogTrace("Waiting for worker thread...");
            _stoppedEvent.Wait();

            foreach (KeyValuePair<string, ISession> sessionKvp in _sessions)
            {
                sessionKvp.Value.Stop();
            }
            _sessions.Clear();

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
                Task _ = Task.Run(() => AcceptClient(transport, format, createClient, serverComponents, ct), ct);
            }
        }

        private async Task AcceptClient(
            PolyTransport transport,
            PolyFormat format,
            Func<PolyChannel> createClient,
            ServerComponents serverComponents,
            CancellationToken ct)
        {
            ISession session = null;
            try
            {
                PolyChannel client = createClient();
                session = new Session(_serviceProvider, _loggerFactory, _bufferPool, transport, format, client);
                if (!_sessions.TryAdd(session.ID, session))
                    _logger.LogCritical("Failed to start tracking session with ID {0}.", session.ID);

                await session.Start(serverComponents, ct).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failed to start session.");
            }
            finally
            {
                if (session != null)
                {
                    session.Stop();
                    bool isRemoved = _sessions.TryRemove(session.ID, out _);
                    if (!isRemoved)
                        _logger.LogCritical("Failed to stop tracking session with ID {0}.", session.ID);
                }
            }
        }

        public void Stop()
        {
            Dispose();
        }

        public IEnumerable<ISession> GetSessions()
        {
            return new List<ISession>(_sessions.Select(kvp => kvp.Value));
        }
    }
}
