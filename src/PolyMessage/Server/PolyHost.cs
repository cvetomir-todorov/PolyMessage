using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyMessage.CodeGeneration;
using PolyMessage.Messaging;
using PolyMessage.Metadata;
using PolyMessage.Timer;

namespace PolyMessage.Server
{
    /// <summary>
    /// Creates a host for communicating via a certain <see cref="PolyTransport"/>  with messages in a certain <see cref="PolyFormat"/>.
    /// </summary>
    public sealed class PolyHost : IDisposable
    {
        // .net core integration
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        // transport/format
        private readonly PolyTransport _transport;
        private readonly PolyFormat _format;
        // metadata
        private readonly List<Operation> _operations;
        private readonly IContractInspector _contractInspector;
        // server
        private readonly ArrayPool<byte> _bufferPool;
        private readonly ITimer _timer;
        private IAcceptor _acceptor;
        // stop/dispose
        private readonly CancellationTokenSource _stopTokenSource;
        private bool _isDisposed;

        public PolyHost(PolyTransport transport, PolyFormat format, IServiceProvider serviceProvider)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (format == null)
                throw new ArgumentNullException(nameof(format));
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            // .net core integration
            _serviceProvider = serviceProvider;
            _loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = _loggerFactory.CreateLogger(GetType());
            // transport/format
            _transport = transport;
            _format = format;
            // metadata
            _operations = new List<Operation>();
            _contractInspector = new ContractInspector(_loggerFactory);
            // server
            _bufferPool = ArrayPool<byte>.Create(maxArrayLength: transport.MessageBufferSettings.MaxSize, maxArraysPerBucket: 128);
            ILogger timerLogger = _loggerFactory.CreateLogger<ITimer>();
            _timer = new HashedWheelTimer(timerLogger, tickDuration: TimeSpan.FromMilliseconds(50), ticksPerWheel: 100000, maxPendingTimeouts: long.MaxValue);
            // stop/dispose
            _stopTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _timer.Dispose();
            _stopTokenSource.Cancel();
            _acceptor?.Stop();

            _stopTokenSource.Dispose();
            _acceptor?.Dispose();

            _isDisposed = true;
            _logger.LogInformation("Stopped.");
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException("Host is already stopped.");
        }

        public void AddContract<TContract>() where TContract : class
        {
            AddContract(typeof(TContract));
        }

        public void AddContract(Type contractType)
        {
            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            EnsureNotDisposed();
            IEnumerable<Operation> operations = _contractInspector.InspectContract(contractType);
            _operations.AddRange(operations);
        }

        public Task StartAsync()
        {
            EnsureNotDisposed();
            if (_operations.Count <= 0)
                throw new InvalidOperationException("No contracts added.");

            _acceptor = new Acceptor(_serviceProvider, _loggerFactory, _bufferPool);
            IMessageMetadata messageMetadata = new MessageMetadata();
            IRouter router = new Router();
            ICodeGenerator codeGenerator = new ILEmitter();

            messageMetadata.Build(_operations);
            router.BuildRoutingTable(_operations);
            codeGenerator.GenerateCode(_operations);
            RegisterMessageTypes();

            IMessenger messenger = new Messenger(_loggerFactory, messageMetadata);
            IDispatcher dispatcher = new Dispatcher(messageMetadata, codeGenerator.GetDispatchRequest());
            ServerComponents serverComponents = new ServerComponents(router, messageMetadata, messenger, _timer, dispatcher);

            Task serverTask = Task.Run(async () => await _acceptor.Start(_transport, _format, serverComponents, _stopTokenSource.Token).ConfigureAwait(false));
            LogHostInfo();
            return serverTask;
        }

        private void RegisterMessageTypes()
        {
            IEnumerable<MessageInfo> requestTypes = _operations.Select(o => new MessageInfo(o.RequestType, o.RequestTypeID));
            IEnumerable<MessageInfo> responseTypes = _operations.Select(o => new MessageInfo(o.ResponseType, o.ResponseTypeID));
            IEnumerable<MessageInfo> systemTypes = new[] { new MessageInfo(typeof(PolyHeader), PolyHeader.TypeID) };
            IEnumerable<MessageInfo> allTypes = systemTypes.Union(requestTypes).Union(responseTypes);
            _format.RegisterMessageTypes(allTypes);
        }

        private void LogHostInfo()
        {
            _logger.LogInformation(
                "Started host using {0} transport listening at {1} and {2} format with {3} operation(s).",
                _transport.DisplayName, _transport.Address, _format.DisplayName, _operations.Count);

            string transportSettings = _transport.GetSettingsInfo();
            if (!string.IsNullOrWhiteSpace(transportSettings))
                _logger.LogInformation("Transport {0} settings: {1}.", _transport.DisplayName, transportSettings);
        }

        public void Stop()
        {
            Dispose();
        }

        internal IEnumerable<PolyChannel> GetConnectedClients()
        {
            return _acceptor.GetSessions().Select(session => session.ConnectedClient);
        }
    }
}
