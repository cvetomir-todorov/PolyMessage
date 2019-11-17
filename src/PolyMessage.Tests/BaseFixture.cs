using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyMessage.Binary;
using PolyMessage.Tcp;
using Xunit.Abstractions;

namespace PolyMessage.Tests
{
    public abstract class BaseFixture : IDisposable
    {
        protected IServiceProvider ServiceProvider { get; }
        protected ILogger Logger { get; }
        protected Uri ServerAddress { get; }
        protected PolyHost Host { get; }
        protected List<PolyClient> Clients { get; }

        protected BaseFixture(ITestOutputHelper output) : this(output, collection => {})
        {}

        protected BaseFixture(ITestOutputHelper output, Action<IServiceCollection> addServices)
        {
            ServiceProvider = CreateServiceProvider(output, addServices);
            Logger = ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());

            ServerAddress = GetServerAddress();
            Host = CreateHost(ServerAddress, ServiceProvider);
            Clients = new List<PolyClient>();
        }

        public void Dispose()
        {
            Dispose(disposingInsteadOfFinalizing: true);
        }

        protected virtual void Dispose(bool disposingInsteadOfFinalizing)
        {
            if (disposingInsteadOfFinalizing)
            {
                foreach (PolyClient client in Clients)
                {
                    client.Dispose();
                }
                Host.Stop();
            }
        }

        private static IServiceProvider CreateServiceProvider(ITestOutputHelper output, Action<IServiceCollection> addServices)
        {
            IServiceCollection services = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.SetMinimumLevel(LogLevel.Information);
                    loggingBuilder.AddDebug();
                    loggingBuilder.AddProvider(new XunitLoggingProvider(output));
                });
            addServices(services);
            return services.BuildServiceProvider();
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

        protected static PolyClient CreateClient(Uri serverAddress, IServiceProvider serviceProvider)
        {
            ITransport clientTransport = new TcpTransport(serverAddress);
            IFormat clientFormat = new BinaryFormat();
            PolyClient client = new PolyClient(clientTransport, clientFormat, serviceProvider.GetRequiredService<ILoggerFactory>());
            return client;
        }

        protected async Task StartHost()
        {
            Host.Start();
            await Task.Delay(100);
        }
    }
}
