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

namespace PolyMessage.Tests.Integration
{
    public abstract class BaseIntegrationFixture : BaseFixture
    {
        protected Uri ServerAddress { get; }
        protected PolyHost Host { get; }
        protected List<PolyClient> Clients { get; }

        protected BaseIntegrationFixture(ITestOutputHelper output) : this(output, collection => {})
        {}

        protected BaseIntegrationFixture(ITestOutputHelper output, Action<IServiceCollection> addServices) : base(output, addServices)
        {
            ServerAddress = GetServerAddress();
            Host = CreateHost(ServerAddress, ServiceProvider);
            Clients = new List<PolyClient>();
        }

        protected override void Dispose(bool disposingInsteadOfFinalizing)
        {
            if (disposingInsteadOfFinalizing)
            {
                foreach (PolyClient client in Clients)
                {
                    client.Dispose();
                }
                Host.Stop();
            }

            base.Dispose(disposingInsteadOfFinalizing);
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
            PolyTransport hostTransport = new TcpTransport(serverAddress);
            PolyFormat hostFormat = new BinaryFormat();
            PolyHost host = new PolyHost(hostTransport, hostFormat, serviceProvider);
            return host;
        }

        protected static PolyClient CreateClient(Uri serverAddress, IServiceProvider serviceProvider)
        {
            PolyTransport clientTransport = new TcpTransport(serverAddress);
            PolyFormat clientFormat = new BinaryFormat();
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
