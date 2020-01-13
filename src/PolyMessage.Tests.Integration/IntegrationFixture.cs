using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration
{
    public abstract class IntegrationFixture : BaseFixture
    {
        protected Uri ServerAddress { get; }
        protected PolyTransport HostTransport { get; private set; }
        protected PolyHost Host { get; }
        protected PolyTransport ClientTransport { get; private set; }
        protected PolyClient Client { get; set; }
        protected List<PolyClient> Clients { get; }

        protected IntegrationFixture(ITestOutputHelper output) : this(output, collection => {})
        {}

        protected IntegrationFixture(ITestOutputHelper output, Action<IServiceCollection> addServices) : base(output, addServices)
        {
            ServerAddress = GetServerAddress();
            Host = CreateHost(ServerAddress, ServiceProvider);
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

        protected abstract PolyTransport CreateTransport(Uri serverAddress);
        private PolyTransport CreateHostTransport(Uri serverAddress) => CreateTransport(serverAddress);
        private PolyTransport CreateClientTransport(Uri serverAddress) => CreateTransport(serverAddress);

        private static Uri GetServerAddress()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);
            IPAddress ipv4Address = addresses.First(a => a.AddressFamily == AddressFamily.InterNetwork);

            UriBuilder addressBuilder = new UriBuilder("tcp", ipv4Address.ToString(), 10678);
            return addressBuilder.Uri;
        }

        private PolyHost CreateHost(Uri serverAddress, IServiceProvider serviceProvider)
        {
            HostTransport = CreateHostTransport(serverAddress);
            PolyFormat hostFormat = CreateHostFormat();
            PolyHost host = new PolyHost(HostTransport, hostFormat, serviceProvider);
            return host;
        }

        protected PolyClient CreateClient()
        {
            return CreateClient(ServerAddress, ServiceProvider);
        }

        protected PolyClient CreateClient(Uri serverAddress, IServiceProvider serviceProvider)
        {
            ClientTransport = CreateClientTransport(serverAddress);
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
