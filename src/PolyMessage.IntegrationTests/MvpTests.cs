using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyMessage.Binary;
using PolyMessage.Tcp;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.IntegrationTests
{
    public sealed class MvpTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public MvpTests(ITestOutputHelper output)
        {
            IServiceCollection services = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Information);
                    builder.AddDebug();
                    builder.AddProvider(new XunitLoggingProvider(output));
                });
            services.AddScoped<IStringContract, StringImplementor>();

            _serviceProvider = services.BuildServiceProvider();
            _loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = _loggerFactory.CreateLogger(GetType());
        }

        [Fact]
        public async Task ShouldSendAndReceiveMessage()
        {
            Uri serverAddress = new Uri("tcp://127.0.0.1:10678");
            const int clientsCount = 5;
            const int requestsCount = 1000;
            PolyHost host = null;

            try
            {
                host = StartHost(serverAddress);
                await Task.Delay(1000);

                List<Task> clientTasks = new List<Task>();
                for (int i = 0; i < clientsCount; ++i)
                {
                    Task clientTask = Task.Run(async () =>
                    {
                        PolyProxy client = CreateClient(serverAddress);
                        TimeSpan duration = await ConnectMakeRequestsDisconnect(client, requestsCount);
                        _logger.LogInformation("Making {0} requests from a proxy took: {1:0} ms.", requestsCount, duration.TotalMilliseconds);
                    });
                    clientTasks.Add(clientTask);
                }

                Task.WaitAll(clientTasks.ToArray());
            }
            finally
            {
                host?.Stop();
            }
        }

        private PolyHost StartHost(Uri serverAddress)
        {
            ITransport hostTransport = new TcpTransport(serverAddress);
            IFormat hostFormat = new BinaryFormat();
            PolyHost host = new PolyHost(hostTransport, hostFormat, _serviceProvider);
            host.AddContract<IStringContract, StringImplementor>();

            host.Start().ContinueWith(task =>
            {
                _logger.LogError("Starting host threw: {0}", task.Exception);
            },
                TaskContinuationOptions.OnlyOnFaulted);
            return host;
        }

        private PolyProxy CreateClient(Uri serverAddress)
        {
            ITransport clientTransport = new TcpTransport(serverAddress);
            IFormat clientFormat = new BinaryFormat();
            PolyProxy proxy = new PolyProxy(clientTransport, clientFormat, _loggerFactory);

            return proxy;
        }

        private async Task<TimeSpan> ConnectMakeRequestsDisconnect(PolyProxy proxy, int requestsCount)
        {
            try
            {
                // currently this creates the proxy and connects to the server
                proxy.AddContract<IStringContract>();

                // warmup
                await proxy.Get<IStringContract>().Call("request");

                Stopwatch requestsWatch = Stopwatch.StartNew();
                for (int i = 0; i < requestsCount; ++i)
                {
                    await proxy.Get<IStringContract>().Call("request");
                }

                requestsWatch.Stop();
                return requestsWatch.Elapsed;
            }
            catch (Exception exception)
            {
                _logger.LogError("Proxy threw: {0}", exception);
                throw;
            }
            finally
            {
                proxy.Dispose();
            }
        }
    }

    [PolyContract]
    public interface IStringContract
    {
        [PolyRequestResponseEndpoint]
        Task<string> Call(string message);
    }

    public sealed class StringImplementor : IStringContract
    {
        public Task<string> Call(string message)
        {
            return Task.FromResult($"[{DateTime.UtcNow:u}] {message}");
        }
    }
}
