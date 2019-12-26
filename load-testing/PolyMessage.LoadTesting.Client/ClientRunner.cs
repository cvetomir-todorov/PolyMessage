using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyMessage.LoadTesting.Contracts;

namespace PolyMessage.LoadTesting.Client
{
    public sealed class ClientRunner
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private string _stringData;
        private List<Dto> _objectsData;

        public ClientRunner(ILogger logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public void Run(ClientOptions options)
        {
            ClientFactory factory = new ClientFactory(_logger, options);
            PolyTransport transport = factory.CreateTransport(_serviceProvider);
            PolyFormat format = factory.CreateFormat();
            ILoggerFactory loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

            PolyClient[] clients = new PolyClient[options.Clients];
            Task[] clientTasks = new Task[options.Clients];

            SetupMessaging(options);

            _logger.LogInformation("Prepare {0} clients.", options.Clients);
            for (int i = 0; i < options.Clients; ++i)
            {
                PolyClient client = new PolyClient(transport, format, loggerFactory);
                client.AddContract<ILoadTestingContract>();
                clients[i] = client;
            }

            _logger.LogInformation("Start {0} clients with {1} transactions each.", options.Clients, options.Transactions);

            for (int i = 0; i < options.Clients; ++i)
            {
                int local = i;
                string clientID = $"LoadTester{i}";
                Task clientTask = Task.Run(() => RunClient(clientID, clients[local], options));
                clientTasks[i] = clientTask;
            }

            Task.WaitAll(clientTasks);
            ShowAllClientResults(clientTasks, options);
        }

        private void SetupMessaging(ClientOptions options)
        {
            switch (options.Messaging)
            {
                case Messaging.Empty:
                    _logger.LogInformation("Messaging is {0}.", Messaging.Empty);
                    break;
                case Messaging.String:
                    _logger.LogInformation("Messaging is {0} with {1} length.", Messaging.String, options.MessagingStringLength);
                    break;
                case Messaging.Objects:
                    _logger.LogInformation("Messaging is {0} with {1} objects.", Messaging.Objects, options.MessagingObjectsCount);
                    break;
            }

            _stringData = GenerateString(options.MessagingStringLength);
            _objectsData = Enumerable.Repeat(new Dto(), options.MessagingObjectsCount).ToList();
        }

        private static string GenerateString(int length)
        {
            StringBuilder builder = new StringBuilder();

            while (builder.Length < length)
            {
                builder.Append(DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
            }

            if (builder.Length > length)
            {
                builder.Remove(length, builder.Length - length);
            }

            return builder.ToString();
        }

        private async Task<ClientRunResult> RunClient(
            string clientID,
            PolyClient client,
            ClientOptions options)
        {
            await client.ConnectAsync();
            ILoadTestingContract contract = client.Get<ILoadTestingContract>();

            EmptyRequest emptyRequest = new EmptyRequest();
            StringRequest stringRequest = new StringRequest {Data = _stringData};
            ObjectsRequest objectsRequest = new ObjectsRequest {Objects = _objectsData};

            _logger.LogDebug("[{0}] Start {1} transactions.", clientID, options.Transactions);

            List<TimeSpan> latencies = new List<TimeSpan>(capacity: options.Transactions);
            Stopwatch latencyStopwatch = new Stopwatch();
            Stopwatch totalTimeStopwatch = Stopwatch.StartNew();

            for (int i = 0; i < options.Transactions; ++i)
            {
                latencyStopwatch.Restart();

                switch (options.Messaging)
                {
                    case Messaging.Empty:
                        await contract.EmptyOperation(emptyRequest).ConfigureAwait(false);
                        break;
                    case Messaging.String:
                        await contract.StringOperation(stringRequest).ConfigureAwait(false);
                        break;
                    case Messaging.Objects:
                        await contract.ObjectsOperation(objectsRequest).ConfigureAwait(false);
                        break;
                }

                latencyStopwatch.Stop();
                latencies.Add(latencyStopwatch.Elapsed);
            }
            totalTimeStopwatch.Stop();

            ClientRunResult result = CreateSingleClientResult(options.Transactions, totalTimeStopwatch.Elapsed, latencies);
            _logger.LogDebug(
                "[{0}] Completed {1} transactions for {2:###0.00} seconds. {3:###0.00} transactions per second. {4:###0.00} ms latency (P99).",
                clientID, options.Transactions, totalTimeStopwatch.Elapsed.TotalSeconds, result.TransactionsPerSecond, result.LatencyP99);
            return result;
        }

        private ClientRunResult CreateSingleClientResult(int transactionCount, TimeSpan totalTime, List<TimeSpan> latencies)
        {
            double transactionsPerSecond = transactionCount / totalTime.TotalSeconds;
            double latencyP99 = latencies
                .Select(latency => latency.TotalMilliseconds)
                .OrderByDescending(latencyMs => latencyMs)
                .Skip(transactionCount / 100)
                .First();

            return new ClientRunResult(transactionsPerSecond, latencyP99, latencies);
        }

        private void ShowAllClientResults(Task[] clientTasks, ClientOptions options)
        {
            if (clientTasks.All(clientTask => clientTask.IsCompletedSuccessfully))
            {
                double transactionsPerSecond = clientTasks
                    .Select(task => (Task<ClientRunResult>)task)
                    .Select(task => task.Result.TransactionsPerSecond)
                    .Average();
                double latencyP99 = clientTasks
                    .Select(task => (Task<ClientRunResult>) task)
                    .SelectMany(task => task.Result.Latencies)
                    .Select(latency => latency.TotalMilliseconds)
                    .OrderByDescending(latencyMs => latencyMs)
                    .Skip(options.Clients * options.Transactions / 100)
                    .First();
                _logger.LogInformation("{0:###0.00} transactions per second. {1:###0.00} ms latency (P99).", transactionsPerSecond, latencyP99);
            }
            else
            {
                _logger.LogError("Some clients failed.");
                foreach (Task clientTask in clientTasks)
                {
                    if (clientTask.Exception != null)
                    {
                        _logger.LogError(clientTask.Exception, "Client task failed.");
                    }
                }
            }
        }

        private struct ClientRunResult
        {
            public ClientRunResult(double transactionsPerSecond, double latencyP99, IReadOnlyCollection<TimeSpan> latencies)
            {
                TransactionsPerSecond = transactionsPerSecond;
                LatencyP99 = latencyP99;
                Latencies = latencies;
            }

            public double TransactionsPerSecond { get; }
            public double LatencyP99 { get; }
            public IReadOnlyCollection<TimeSpan> Latencies { get; }
        }
    }
}
