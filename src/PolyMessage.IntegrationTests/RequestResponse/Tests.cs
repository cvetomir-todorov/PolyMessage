using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyMessage.Binary;
using PolyMessage.Tcp;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.IntegrationTests.RequestResponse
{
    public class Tests : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly Uri _serverAddress;
        private readonly PolyHost _host;
        private readonly List<PolyProxy> _clients;

        public Tests(ITestOutputHelper output)
        {
            _serviceProvider = CreateServiceProvider(output);
            _logger = _serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());

            _serverAddress = GetServerAddress();
            _host = CreateHost(_serverAddress, _serviceProvider);
            _clients = new List<PolyProxy>();
        }

        public void Dispose()
        {
            foreach (PolyProxy client in _clients)
            {
                client.Dispose();
            }
            _host.Dispose();
        }

        private IServiceProvider CreateServiceProvider(ITestOutputHelper output)
        {
            IServiceCollection services = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.SetMinimumLevel(LogLevel.Information);
                    loggingBuilder.AddDebug();
                    loggingBuilder.AddProvider(new XunitLoggingProvider(output));
                });
            services.AddScoped<IStringContract, StringImplementor>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }

        private static Uri GetServerAddress()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);
            IPAddress ipv4Address = addresses.First(a => a.AddressFamily == AddressFamily.InterNetwork);

            UriBuilder addressBuilder = new UriBuilder("tcp", ipv4Address.ToString(), 10678);
            return addressBuilder.Uri;
        }

        private static PolyHost CreateHost(Uri serverAddress, IServiceProvider serviceProvider)
        {
            ITransport hostTransport = new TcpTransport(serverAddress);
            IFormat hostFormat = new BinaryFormat();
            PolyHost host = new PolyHost(hostTransport, hostFormat, serviceProvider);
            return host;
        }

        private static PolyProxy CreateClient(Uri serverAddress, ILoggerFactory loggerFactory)
        {
            ITransport clientTransport = new TcpTransport(serverAddress);
            IFormat clientFormat = new BinaryFormat();
            PolyProxy client = new PolyProxy(clientTransport, clientFormat, loggerFactory);
            return client;
        }

        private async Task StartHost()
        {
            _host.Start();
            await Task.Delay(100);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 100)]
        [InlineData(2, 1)]
        [InlineData(2, 100)]
        [InlineData(10, 100)]
        public async Task ShouldCommunicate(int clientsCount, int messagesCount)
        {
            // arrange
            for (int i = 0; i < clientsCount; ++i)
            {
                PolyProxy client = CreateClient(_serverAddress, _serviceProvider.GetRequiredService<ILoggerFactory>());
                _clients.Add(client);
            }

            // act
            _host.AddContract<IStringContract, StringImplementor>();
            await StartHost();

            List<Task<double>> clientTasks = new List<Task<double>>();
            foreach (PolyProxy client in _clients)
            {
                Task<double> clientTask = Task.Run(async () =>
                {
                    TimeSpan duration = await MakeRequests(client, messagesCount);
                    _logger.LogInformation("Making {0} requests from a proxy took: {1:0} ms.", messagesCount, duration.TotalMilliseconds);
                    return duration.TotalMilliseconds;
                });
                clientTasks.Add(clientTask);
            }

            Task.WaitAll(clientTasks.ToArray(), TimeSpan.FromSeconds(10));

            // assert
            using (new AssertionScope())
            {
                int succeededTasks = clientTasks.Count(ct => ct.IsCompletedSuccessfully);
                succeededTasks.Should().Be(clientTasks.Count);

                foreach (Task<double> clientTask in clientTasks)
                {
                    clientTask.Exception.Should().BeNull();
                    double totalDuration = clientTask.Result;
                    double durationPerRequest = totalDuration / messagesCount;
                    durationPerRequest.Should().BeLessOrEqualTo(1.0);
                }
            }
        }

        private async Task<TimeSpan> MakeRequests(PolyProxy client, int messagesCount)
        {
            // currently this creates the proxy and connects to the server
            client.AddContract<IStringContract>();
            IStringContract proxy = client.Get<IStringContract>();

            // warmup
            const string requestMessage = "request";
            await proxy.Call1(requestMessage);

            Stopwatch requestsWatch = Stopwatch.StartNew();
            for (int i = 0; i < messagesCount; ++i)
            {
                await proxy.Call1(requestMessage);
            }

            requestsWatch.Stop();
            return requestsWatch.Elapsed;
        }
    }
}
