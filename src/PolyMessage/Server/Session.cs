using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Exceptions;
using PolyMessage.Metadata;
using PolyMessage.Timer;

namespace PolyMessage.Server
{
    internal interface ISession : IDisposable
    {
        string ID { get; }

        Task Start(ServerComponents serverComponents, CancellationToken ct);

        void Stop();

        PolyChannel ConnectedClient { get; }
    }

    internal sealed class Session : ISession
    {
        private readonly ILogger _logger;
        private readonly PolyTransport _transport;
        private readonly PolyFormatter _formatter;
        private readonly PolyChannel _connectedClient;
        private readonly IImplementorProvider _implementorProvider;
        // timeout timer tasks
        private readonly ITimerTask _receiveTimerTask;
        private readonly ITimerTask _sendTimerTask;
        private ITimeout _clientIOTimeout;
        // identity
        private static int _generation;
        private readonly string _id;
        // stop/dispose
        private readonly ManualResetEventSlim _stoppedEvent;
        private readonly object _disposeLock;
        private volatile bool _isDisposed;
        private bool _isStopRequested;

        public Session(
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            PolyTransport transport,
            PolyFormat format,
            PolyChannel connectedClient)
        {
            // identity
            _id = "Session" + Interlocked.Increment(ref _generation);

            _logger = loggerFactory.CreateLogger(GetType());
            _transport = transport;
            _formatter = format.CreateFormatter();
            _connectedClient = connectedClient;
            _implementorProvider = new ImplementorProvider(serviceProvider);
            // timeout timer tasks
            _receiveTimerTask = new DisposeSessionTimerTask(this, _logger, "[{0}] Client receive timeout.", new object[] {_id});
            _sendTimerTask = new DisposeSessionTimerTask(this, _logger, "[{0}] Client send timeout.", new object[] {_id});
            // stop/dispose
            _stoppedEvent = new ManualResetEventSlim(initialState: true);
            _disposeLock = new object();
        }

        public void Dispose()
        {
            // it is possible for the session to be stopped from different threads
            if (!_isDisposed)
                lock (_disposeLock)
                    if (!_isDisposed)
                    {
                        _isStopRequested = true;
                        _implementorProvider.Dispose();
                        _connectedClient.Close();
                        _formatter.Dispose();
                        _clientIOTimeout?.Cancel();
                        _logger.LogTrace("[{0}] Waiting for worker thread...", _id);
                        _stoppedEvent.Wait();
                        _stoppedEvent.Dispose();

                        _isDisposed = true;
                        _logger.LogTrace("[{0}] Stopped.", _id);
                    }
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Session is already stopped.");
        }

        public string ID => _id;

        public async Task Start(ServerComponents serverComponents, CancellationToken ct)
        {
            EnsureNotDisposed();

            try
            {
                _stoppedEvent.Reset();
                await DoStart(serverComponents, ct).ConfigureAwait(false);
            }
            catch (PolyConnectionClosedException connectionClosedException)
                when (connectionClosedException.CloseReason == PolyConnectionCloseReason.ConnectionClosed)
            {
                _logger.LogDebug("[{0}] Connection has been closed. Reason is {1}.", _id, connectionClosedException.CloseReason);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "[{0}] Unexpected error occurred.", _id);
            }
            finally
            {
                _stoppedEvent.Set();
                _logger.LogTrace("[{0}] Stopped worker thread.", _id);
            }
        }

        private async Task DoStart(ServerComponents serverComponents, CancellationToken ct)
        {
            await _connectedClient.OpenAsync().ConfigureAwait(false);
            _implementorProvider.SessionStarted(_connectedClient);

            while (!ct.IsCancellationRequested && !_isStopRequested)
            {
                object requestMessage = await ReceiveRequest(serverComponents, ct).ConfigureAwait(false);
                object responseMessage = await DispatchMessage(serverComponents, requestMessage).ConfigureAwait(false);
                await SendResponse(serverComponents, ct, responseMessage).ConfigureAwait(false);
            }
        }

        private async Task<object> ReceiveRequest(ServerComponents serverComponents, CancellationToken ct)
        {
            if (_transport.HostTimeouts.ClientReceive > TimeSpan.Zero)
            {
                _clientIOTimeout = serverComponents.Timer.NewTimeout(_receiveTimerTask, _transport.HostTimeouts.ClientReceive);
            }

            object requestMessage = await _connectedClient.Receive(_formatter, _id, ct).ConfigureAwait(false);

            if (_clientIOTimeout != null)
            {
                _clientIOTimeout.Cancel();
                _clientIOTimeout = null;
            }

            _logger.LogTrace("[{0}] Received request [{1}]", _id, requestMessage.GetType());
            return requestMessage;
        }

        private async Task SendResponse(ServerComponents serverComponents, CancellationToken ct, object responseMessage)
        {
            _logger.LogTrace("[{0}] Sending response [{1}]...", _id, responseMessage.GetType());

            if (_transport.HostTimeouts.ClientSend > TimeSpan.Zero)
            {
                // TODO: figure out how to test the client send timeout
                _clientIOTimeout = serverComponents.Timer.NewTimeout(_sendTimerTask, _transport.HostTimeouts.ClientSend);
            }

            await _connectedClient.Send(responseMessage, _formatter, _id, ct).ConfigureAwait(false);

            if (_clientIOTimeout != null)
            {
                _clientIOTimeout.Cancel();
                _clientIOTimeout = null;
            }

            _logger.LogTrace("[{0}] Sent response [{1}]", _id, responseMessage.GetType());
        }

        private async Task<object> DispatchMessage(ServerComponents serverComponents, object requestMessage)
        {
            Operation operation = serverComponents.Router.ChooseOperation(requestMessage, serverComponents.MessageMetadata);
            try
            {
                _implementorProvider.OperationStarted();
                object implementor = _implementorProvider.ResolveImplementor(operation.ContractType);
                object responseMessage = await serverComponents.Dispatcher.Dispatch(implementor, requestMessage, operation).ConfigureAwait(false);
                return responseMessage;
            }
            finally
            {
                _implementorProvider.OperationFinished();
            }
        }

        public void Stop()
        {
            Dispose();
        }

        public PolyChannel ConnectedClient => _connectedClient;

        private class DisposeSessionTimerTask : ITimerTask
        {
            private readonly ISession _session;
            private readonly ILogger _logger;
            private readonly string _logMessage;
            private readonly object[] _logArgs;

            public DisposeSessionTimerTask(ISession session, ILogger logger, string logMessage, object[] logArgs)
            {
                _session = session;
                _logger = logger;
                _logMessage = logMessage;
                _logArgs = logArgs;
            }

            public void Run(ITimeout timeout)
            {
                _logger.LogDebug(_logMessage, _logArgs);
                _session.Dispose();
            }
        }
    }
}
