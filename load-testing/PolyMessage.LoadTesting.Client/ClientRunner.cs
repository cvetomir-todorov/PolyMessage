using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

            Task[] clientTasks = new Task[options.Clients];

            _logger.LogInformation("Start {0} clients with {1} transactions each.", options.Clients, options.Transactions);
            for (int i = 0; i < options.Clients; ++i)
            {
                PolyClient client = new PolyClient(transport, format, loggerFactory);
                client.AddContract<ILoadTestingContract>();

                string clientID = $"LoadTester{i}";
                Task clientTask = Task.Run(() => RunClient(clientID, client, options));
                clientTasks[i] = clientTask;
            }

            Task.WaitAll(clientTasks);
            ShowAllClientResults(clientTasks, options);
        }

        private async Task<ClientRunResult> RunClient(
            string clientID,
            PolyClient client,
            ClientOptions options)
        {
            await client.ConnectAsync();
            ILoadTestingContract contract = client.Get<ILoadTestingContract>();

            EmptyRequest request = new EmptyRequest();
            List<TimeSpan> latencies = new List<TimeSpan>(capacity: options.Transactions);
            _logger.LogDebug("[{0}] Start {1} transactions.", clientID, options.Transactions);

            Stopwatch totalTimeStopwatch = Stopwatch.StartNew();
            Stopwatch latencyStopwatch = new Stopwatch();
            for (int i = 0; i < options.Transactions; ++i)
            {
                latencyStopwatch.Restart();
                await contract.EmptyOperation(request);
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
