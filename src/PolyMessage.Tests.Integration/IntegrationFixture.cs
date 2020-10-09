using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyMessage.Client;
using PolyMessage.Server;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration
{
    public enum TransportUnderTest
    {
        Tcp, Ipc
    }

    public abstract class IntegrationFixture : BaseFixture
    {
        protected Uri ServerAddress { get; }
        protected PolyTransport HostTransport { get; private set; }
        protected PolyHost Host { get; }
        protected PolyTransport ClientTransport { get; private set; }
        protected PolyClient Client { get; set; }
        protected List<PolyClient> Clients { get; }

        protected IntegrationFixture(ITestOutputHelper output, TransportUnderTest transport) : this(output, transport, collection => {})
        {}

        protected IntegrationFixture(ITestOutputHelper output, TransportUnderTest transport, Action<IServiceCollection> addServices)
            : base(output, addServices)
        {
            ServerAddress = GetServerAddress(transport);
            Host = CreateHost();
            Client = CreateClient();
            Clients = new List<PolyClient>();
        }

        protected override void Dispose(bool disposingInsteadOfFinalizing)
        {
            if (disposingInsteadOfFinalizing)
            {
                Client.Disconnect();
                foreach (PolyClient client in Clients)
                {
                    client.Disconnect();
                }
                Host.Stop();
            }

            base.Dispose(disposingInsteadOfFinalizing);
        }

        protected abstract PolyFormat CreateFormat();
        private PolyFormat CreateHostFormat() => CreateFormat();
        private PolyFormat CreateClientFormat() => CreateFormat();

        protected abstract PolyTransport CreateTransport(Uri address);
        private PolyTransport CreateHostTransport(Uri address) => CreateTransport(address);
        private PolyTransport CreateClientTransport(Uri address) => CreateTransport(address);

        private static Uri GetServerAddress(TransportUnderTest transport)
        {
            switch (transport)
            {
                case TransportUnderTest.Tcp:
                {
                    string hostName = Dns.GetHostName();
                    IPAddress[] addresses = Dns.GetHostAddresses(hostName);
                    IPAddress ipv4Address = addresses.First(a => a.AddressFamily == AddressFamily.InterNetwork);

                    UriBuilder addressBuilder = new UriBuilder("tcp", ipv4Address.ToString(), 10678);
                    return addressBuilder.Uri;
                }
                case TransportUnderTest.Ipc:
                {
                    return new Uri("net.pipe://127.0.0.1/test");
                }
                default: throw new NotSupportedException($"Transport {transport} is not supported.");
            }
        }

        private PolyHost CreateHost()
        {
            HostTransport = CreateHostTransport(ServerAddress);
            PolyFormat hostFormat = CreateHostFormat();
            PolyHost host = new PolyHost(HostTransport, hostFormat, ServiceProvider);
            return host;
        }

        protected PolyClient CreateClient()
        {
            return CreateClient(ServerAddress, ServiceProvider);
        }

        protected PolyClient CreateClient(Uri address, IServiceProvider serviceProvider)
        {
            ClientTransport = CreateClientTransport(address);
            PolyFormat clientFormat = CreateClientFormat();
            PolyClient client = new PolyClient(ClientTransport, clientFormat, serviceProvider.GetRequiredService<ILoggerFactory>());
            return client;
        }

        protected async Task StartHost()
        {
            Task _ = Host.StartAsync();
            await Task.Delay(100);
        }

        protected async Task StartHostAndConnectClient()
        {
            await StartHost();
            await Client.ConnectAsync();
        }
    }
}
