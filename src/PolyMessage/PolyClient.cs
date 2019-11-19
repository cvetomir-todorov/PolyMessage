using System;
using System.Collections.Generic;
using System.Threading;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PolyMessage.CodeGeneration;
using PolyMessage.Messaging;
using PolyMessage.Metadata;
using PolyMessage.Proxies;

namespace PolyMessage
{
    public class PolyClient : IDisposable
    {
        // .net core integration
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        // transport/format
        private readonly PolyTransport _transport;
        private readonly PolyFormat _format;
        // metadata
        private readonly List<Type> _contracts;
        private readonly List<Operation> _operations;
        private readonly IContractInspector _contractInspector;
        private IMessageMetadata _messageMetadata;
        // messaging
        private readonly object _setupMessagingLock;
        private volatile IMessenger _messenger;
        private PolyChannel _channel;
        // proxies
        private readonly IProxyGenerator _proxyGenerator;
        private readonly object _createProxyLock;
        private readonly Dictionary<Type, object> _proxies;
        // identity
        private static int _generation;
        private readonly string _id;
        // stop/dispose
        private readonly CancellationTokenSource _cancelTokenSource;

        public PolyClient(PolyTransport transport, PolyFormat format)
            : this(transport, format, new NullLoggerFactory())
        {}

        public PolyClient(PolyTransport transport, PolyFormat format, ILoggerFactory loggerFactory)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (format == null)
                throw new ArgumentNullException(nameof(format));
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            // .net core integration
            _logger = loggerFactory.CreateLogger(GetType());
            _loggerFactory = loggerFactory;
            // transport/format
            _transport = transport;
            _format = format;
            // metadata
            _contracts = new List<Type>();
            _operations = new List<Operation>();
            _contractInspector = new ContractInspector(loggerFactory);
            // messaging
            _setupMessagingLock = new object();
            // proxies
            _proxyGenerator = new ProxyGenerator();
            _createProxyLock = new object();
            _proxies = new Dictionary<Type, object>();
            // identity
            _id = "Client" + Interlocked.Increment(ref _generation);
            // stop/dispose
            _cancelTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            if (State == CommunicationState.Closed)
                return;

            _cancelTokenSource.Cancel();
            _cancelTokenSource.Dispose();
            _channel?.Dispose();
            _logger.LogInformation("[{0}] Stopped", _id);

            State = CommunicationState.Closed;
        }

        public CommunicationState State { get; private set; }

        private void EnsureState(CommunicationState expectedState, string action)
        {
            if (State != expectedState)
                throw new InvalidOperationException($"[{_id}] should be in {expectedState} state in order to {action}.");
        }

        public void AddContract<TContract>() where TContract : class
        {
            EnsureState(CommunicationState.Created, "add contract");

            Type contractType = typeof(TContract);
            IEnumerable<Operation> operations = _contractInspector.InspectContract(contractType);
            _operations.AddRange(operations);
            _contracts.Add(contractType);
        }

        public void Connect()
        {
            EnsureState(CommunicationState.Created, "connect to the server");

            if (_messenger == null)
                lock (_setupMessagingLock)
                    if (_messenger == null)
                    {
                        _channel = _transport.CreateClient();
                        _logger.LogInformation("[{0}] connected via {1} transport to {2}.", _id, _transport.DisplayName, _transport.Address);
                        _messageMetadata = new MessageMetadata();
                        _messageMetadata.Build(_operations);
                        _messenger = new ProtocolMessenger(_loggerFactory, _messageMetadata);
                        State = CommunicationState.Opened;
                    }
        }

        public TContract Get<TContract>() where TContract : class
        {
            EnsureState(CommunicationState.Opened, "get a proxy for sending messages");

            Type contractType = typeof(TContract);
            if (!_contracts.Contains(contractType))
                throw new InvalidOperationException($"{contractType.Name} should be added before connecting.");

            if (!_proxies.TryGetValue(contractType, out object proxy))
            {
                lock (_createProxyLock)
                {
                    if (!_proxies.TryGetValue(contractType, out proxy))
                    {
                        proxy = CreateProxy(contractType);
                        _proxies.Add(contractType, proxy);
                    }
                }
            }

            TContract contract = (TContract) proxy;
            return contract;
        }

        private object CreateProxy(Type contractType)
        {
            ICodeGenerator codeGenerator = new ILEmitter();
            codeGenerator.GenerateCode(_operations);
            CastToTaskOfResponse castDelegate = codeGenerator.GetCastToTaskOfResponse();

            IInterceptor operationInterceptor = new OperationInterceptor(
                _logger, _id, _messenger, _format, _channel, _cancelTokenSource.Token, _messageMetadata, castDelegate);
            object proxy = _proxyGenerator.CreateInterfaceProxyWithoutTarget(contractType, new Type[0], operationInterceptor);
            return proxy;
        }

        public Uri LocalAddress
        {
            get
            {
                EnsureState(CommunicationState.Opened, "get local address.");
                return _channel.LocalAddress;
            }
        }

        public Uri RemoteAddress
        {
            get
            {
                EnsureState(CommunicationState.Opened, "get remote address.");
                return _channel.RemoteAddress;
            }
        }

        internal PolyTransport Transport => _transport;

        internal PolyChannel Channel => _channel;
    }
}
