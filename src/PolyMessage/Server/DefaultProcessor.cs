using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Endpoints;
using PolyMessage.Transports;

namespace PolyMessage.Server
{
    internal interface IProcessor : IDisposable
    {
        Task Start(IChannel channel, IRouter router, IDispatcher dispatcher, CancellationToken cancelToken);

        void Stop();
    }

    internal sealed class DefaultProcessor : IProcessor
    {
        private readonly ILogger _logger;
        private IChannel _channel;
        // stop/dispose
        private readonly ManualResetEventSlim _stoppedEvent;
        private bool _isDisposed;
        private bool _isStopRequested;

        public DefaultProcessor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _stoppedEvent = new ManualResetEventSlim(initialState: false);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isStopRequested = true;
            _channel.Dispose();
            _logger.LogTrace("Waiting worker thread...");
            _stoppedEvent.Wait();
            _stoppedEvent.Dispose();

            _isDisposed = true;
            _logger.LogTrace("Stopped.");
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException("Processor is already stopped.");
        }

        public async Task Start(IChannel channel, IRouter router, IDispatcher dispatcher, CancellationToken cancelToken)
        {
            EnsureNotDisposed();

            try
            {
                _channel = channel;
                await DoStart(router, dispatcher, cancelToken).ConfigureAwait(false);
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

        private async Task DoStart(IRouter router, IDispatcher dispatcher, CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested && !_isStopRequested)
            {
                string requestMessage = await _channel.Receive(cancelToken).ConfigureAwait(false);
                Endpoint endpoint = router.ChooseEndpoint(requestMessage);
                string responseMessage = await dispatcher.Dispatch(requestMessage, endpoint).ConfigureAwait(false);
                await _channel.Send(responseMessage, cancelToken).ConfigureAwait(false);
            }
        }

        public void Stop()
        {
            Dispose();
        }
    }
}
