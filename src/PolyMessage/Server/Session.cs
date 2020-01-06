using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Messaging;
using PolyMessage.Metadata;

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
        private readonly PolyFormatter _formatter;
        private readonly PolyChannel _connectedClient;
        private readonly MessageStream _messageStream;
        private readonly IImplementorProvider _implementorProvider;
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
            ArrayPool<byte> bufferPool,
            PolyTransport transport,
            PolyFormat format,
            PolyChannel connectedClient)
        {
            // identity
            _id = "Session" + Interlocked.Increment(ref _generation);

            _logger = loggerFactory.CreateLogger(GetType());
            _messageStream = new MessageStream(_id, connectedClient, bufferPool, capacity: transport.MessageBufferSettings.InitialSize, loggerFactory);
            _formatter = format.CreateFormatter(_messageStream);
            _connectedClient = connectedClient;
            _implementorProvider = new ImplementorProvider(serviceProvider);
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
                        _messageStream.Close();
                        _formatter.Dispose();
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
            catch (PolyFormatException formatException) when (formatException.FormatError == PolyFormatError.EndOfDataStream)
            {
                _logger.LogDebug("[{0}] Connection has been closed. Format error is {1}.", _id, formatException.FormatError);
            }
            catch (PolyConnectionClosedException connectionClosedException) when (connectionClosedException.CloseReason == PolyConnectionCloseReason.RemoteTimedOut)
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
                object requestMessage = await serverComponents.Messenger.Receive(_id, _messageStream, _formatter, ct).ConfigureAwait(false);
                _logger.LogTrace("[{0}] Received request [{1}]", _id, requestMessage.GetType());

                object responseMessage = await DispatchMessage(serverComponents, requestMessage).ConfigureAwait(false);

                _logger.LogTrace("[{0}] Sending response [{1}]...", _id, responseMessage.GetType());
                await serverComponents.Messenger.Send(_id, responseMessage, _messageStream, _formatter, ct).ConfigureAwait(false);
                _logger.LogTrace("[{0}] Sent response [{1}]", _id, responseMessage.GetType());
            }
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
    }
}
