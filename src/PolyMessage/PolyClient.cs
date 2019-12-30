using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PolyMessage.CodeGeneration;
using PolyMessage.Messaging;
using PolyMessage.Metadata;
using PolyMessage.Proxy;

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
        private readonly SemaphoreSlim _setupMessagingLock;
        private volatile IMessenger _messenger;
        private PolyChannel _channel;
        private PolyConnection _connection;
        private static readonly ArrayPool<byte> _bufferPool;
        // proxies
        private readonly IProxyGenerator _proxyGenerator;
        private readonly object _createProxyLock;
        private readonly Dictionary<Type, object> _proxies;
        private OperationInterceptor _operationInterceptor;
        // identity
        private static int _generation;
        private readonly string _id;
        // stop/dispose
        private readonly CancellationTokenSource _disconnectTokenSource;
        private bool _isDisposed;

        static PolyClient()
        {
            _bufferPool = ArrayPool<byte>.Create(maxArrayLength: int.MaxValue, maxArraysPerBucket: 128);
        }

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
            _setupMessagingLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
            _connection = new PolyConnection();
            // proxies
            _proxyGenerator = new ProxyGenerator();
            _createProxyLock = new object();
            _proxies = new Dictionary<Type, object>();
            // identity
            _id = "Client" + Interlocked.Increment(ref _generation);
            // stop/dispose
            _disconnectTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _disconnectTokenSource.Cancel();
            _disconnectTokenSource.Dispose();
            if (_channel == null)
            {
                _connection.SetClosed();
            }
            else
            {
                _channel.Close();
            }
            _setupMessagingLock.Dispose();
            _operationInterceptor?.Dispose();
            _logger.LogDebug("[{0}] Stopped", _id);

            _isDisposed = true;
        }

        public PolyConnection Connection => _connection;

        private void EnsureState(PolyConnectionState expectedState, string action)
        {
            if (_connection.State != expectedState)
                throw new InvalidOperationException($"[{_id}] should be in {expectedState} state in order to {action}.");
        }

        public void AddContract<TContract>() where TContract : class
        {
            AddContract(typeof(TContract));
        }

        public void AddContract(Type contractType)
        {
            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            EnsureState(PolyConnectionState.Created, "add contract");

            IEnumerable<Operation> operations = _contractInspector.InspectContract(contractType);
            _operations.AddRange(operations);
            _contracts.Add(contractType);
        }

        public async Task ConnectAsync()
        {
            EnsureState(PolyConnectionState.Created, "connect to the server");
            if (_operations.Count <= 0)
                throw new InvalidOperationException("No contracts added.");

            if (_messenger == null)
            {
                try
                {
                    await _setupMessagingLock.WaitAsync();
                    if (_messenger == null)
                    {
                        _channel = _transport.CreateClient();
                        await _channel.OpenAsync();

                        LogConnectionInfo();

                        _connection = _channel.Connection;
                        _messageMetadata = new MessageMetadata();
                        _messageMetadata.Build(_operations);
                        _messenger = new Messenger(_loggerFactory, _messageMetadata);
                        RegisterMessageTypes();
                    }
                }
                finally
                {
                    _setupMessagingLock.Release();
                }
            }
        }

        private void LogConnectionInfo()
        {
            _logger.LogDebug(
                "[{0}] Connected via {1} transport to {2} using {3} format with {4} operation(s).",
                _id, _transport.DisplayName, _transport.Address, _format.DisplayName, _operations.Count);

            string transportSettings = _transport.GetSettingsInfo();
            if (!string.IsNullOrWhiteSpace(transportSettings))
                _logger.LogDebug("[{0}] Transport {1} settings: {2}.", _id, _transport.DisplayName, transportSettings);
        }

        private void RegisterMessageTypes()
        {
            IEnumerable<MessageInfo> requestTypes = _operations.Select(o => new MessageInfo(o.RequestType, o.RequestTypeID));
            IEnumerable<MessageInfo> responseTypes = _operations.Select(o => new MessageInfo(o.ResponseType, o.ResponseTypeID));
            IEnumerable<MessageInfo> systemTypes = new[] {new MessageInfo(typeof(PolyHeader), PolyHeader.TypeID)};
            IEnumerable<MessageInfo> allTypes = systemTypes.Union(requestTypes).Union(responseTypes);
            _format.RegisterMessageTypes(allTypes);
        }

        public TContract Get<TContract>() where TContract : class
        {
            EnsureState(PolyConnectionState.Opened, "get a proxy for sending messages");

            Type contractType = typeof(TContract);
            if (!_contracts.Contains(contractType))
                throw new InvalidOperationException($"{contractType.Name} should be added before connecting.");

            if (!_proxies.TryGetValue(contractType, out object proxy))
                lock (_createProxyLock)
                    if (!_proxies.TryGetValue(contractType, out proxy))
                    {
                        proxy = CreateProxy(contractType);
                        _proxies.Add(contractType, proxy);
                    }

            TContract contract = (TContract) proxy;
            return contract;
        }

        private object CreateProxy(Type contractType)
        {
            ICodeGenerator codeGenerator = new ILEmitter();
            codeGenerator.GenerateCode(_operations);
            CastToTaskOfResponse castDelegate = codeGenerator.GetCastToTaskOfResponse();

            _operationInterceptor = new OperationInterceptor(
                _loggerFactory, _id, _messenger, _format, _channel, _bufferPool, _disconnectTokenSource.Token, _messageMetadata, castDelegate);
            ConnectionPropertyInterceptor connectionPropertyInterceptor = new ConnectionPropertyInterceptor(_channel);

            object proxy = _proxyGenerator.CreateInterfaceProxyWithoutTarget(
                contractType, new Type[0], _operationInterceptor, connectionPropertyInterceptor);
            return proxy;
        }

        public void Disconnect()
        {
            Dispose();
        }
    }
}
