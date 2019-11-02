using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Binary;
using PolyMessage.Formats;
using PolyMessage.Tcp;
using PolyMessage.Transports;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.IntegrationTests
{
    public sealed class MvpTests
    {
        private readonly ITestOutputHelper _output;

        public MvpTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task ShouldSendAndReceiveMessage()
        {
            Uri serverAddress = new Uri("tcp://127.0.0.1:10678");
            PolyHost host = null;

            try
            {
                host = StartHost(serverAddress);
                //ParallelLoopResult result = Parallel.ForEach(requests, async request =>
                //{
                //    await SendRequest(serverAddress, request, 100, random);
                //});

                const int clientsCount = 1;
                for (int i = 0; i < clientsCount; ++i)
                {
                    await MakeSingleClientRequests(serverAddress, requestsCount: 10000);
                }
            }
            finally
            {
                host?.Stop();
            }
        }

        private PolyHost StartHost(Uri serverAddress)
        {
            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggingProvider(_output));

            ITransport hostTransport = new TcpTransport(serverAddress);
            IFormat hostFormat = new BinaryFormat();
            PolyHost host = new PolyHost(loggerFactory, hostTransport, hostFormat);
            host.AddContract<IStringContract, StringImplementor>();

            Task _ = Task.Run(async () =>
            {
                try
                {
                    await host.Start();
                }
                catch (Exception exception)
                {
                    _output.WriteLine("Starting host threw: {0}", exception);
                }
            });
            return host;
        }

        private async Task MakeSingleClientRequests(Uri serverAddress, int requestsCount)
        {
            ITransport clientTransport = new TcpTransport(serverAddress);
            IFormat clientFormat = new BinaryFormat();
            PolyProxy proxy = new PolyProxy(clientTransport, clientFormat);

            try
            {
                // currently this creates the proxy and connects to the server
                proxy.AddContract<IStringContract>();
                await MakeRequests(proxy, requestsCount);
            }
            catch (Exception exception)
            {
                _output.WriteLine("Proxy threw: {0}", exception);
            }
            finally
            {
                proxy.Dispose();
            }
        }

        private async Task MakeRequests(PolyProxy proxy, int requestsCount)
        {
            Stopwatch requestsWatch = Stopwatch.StartNew();
            for (int i = 0; i < requestsCount; ++i)
            {
                string _ = await proxy.Get<IStringContract>().Call("request");
            }

            requestsWatch.Stop();
            _output.WriteLine("Making {0} requests from a proxy took: {1:000000} ms.", requestsCount, requestsWatch.Elapsed.TotalMilliseconds);
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
