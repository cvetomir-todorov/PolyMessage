﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Metadata;

namespace PolyMessage.Server
{
    internal interface IProcessor : IDisposable
    {
        Task Start(ServerComponents serverComponents, CancellationToken cancelToken);

        void Stop();

        PolyChannel ConnectedClient { get; }
    }

    internal sealed class Processor : IProcessor
    {
        private readonly ILogger _logger;
        private readonly PolyFormat _format;
        private readonly PolyChannel _channel;
        // identity
        private static int _generation;
        private readonly string _id;
        // stop/dispose
        private readonly ManualResetEventSlim _stoppedEvent;
        private bool _isDisposed;
        private bool _isStopRequested;

        public Processor(ILoggerFactory loggerFactory, PolyFormat format, PolyChannel channel)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _format = format;
            _channel = channel;
            // identity
            _id = "Processor" + Interlocked.Increment(ref _generation);
            // stop/dispose
            _stoppedEvent = new ManualResetEventSlim(initialState: false);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isStopRequested = true;
            _channel.Dispose();
            _logger.LogTrace("[{0}] Waiting worker thread...", _id);
            _stoppedEvent.Wait();
            _stoppedEvent.Dispose();

            _isDisposed = true;
            _logger.LogTrace("[{0}] Stopped.", _id);
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException("Processor is already stopped.");
        }

        public async Task Start(ServerComponents serverComponents, CancellationToken cancelToken)
        {
            EnsureNotDisposed();

            try
            {
                await DoStart(serverComponents, cancelToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError("[{0}] Unexpected: {1}", _id, exception);
            }
            finally
            {
                _stoppedEvent.Set();
                _logger.LogTrace("[{0}] Stopped worker thread.", _id);
            }
        }

        private async Task DoStart(ServerComponents serverComponents, CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested && !_isStopRequested)
            {
                _logger.LogTrace("[{0}] Receiving request...", _id);
                object requestMessage = await serverComponents.Messenger.Receive(_format, _channel, cancelToken).ConfigureAwait(false);
                _logger.LogTrace("[{0}] Received request [{1}]", _id, requestMessage);

                // TODO: try/catch here to avoid client hanging when infinite timeout is set
                Operation operation = serverComponents.Router.ChooseOperation(requestMessage, serverComponents.MessageMetadata);
                object responseMessage = await serverComponents.Dispatcher.Dispatch(requestMessage, operation).ConfigureAwait(false);

                _logger.LogTrace("[{0}] Sending response [{1}]...", _id, responseMessage);
                await serverComponents.Messenger.Send(responseMessage, _format, _channel, cancelToken).ConfigureAwait(false);
                _logger.LogTrace("[{0}] Sent response [{1}]", _id, responseMessage);
            }
        }

        public void Stop()
        {
            Dispose();
        }

        public PolyChannel ConnectedClient
        {
            get { return _channel; }
        }
    }
}
