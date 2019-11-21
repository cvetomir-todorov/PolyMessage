using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using PolyMessage.Tcp;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Server
{
    public class ServerTests : BaseIntegrationFixture
    {
        private readonly TcpTransport _hostTransport;

        public ServerTests(ITestOutputHelper output) : base(output, services =>
        {
            services.AddScoped<IContract, Implementor>();
        })
        {
            _hostTransport = HostTransport as TcpTransport;
            Host.AddContract<IContract>();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        public async Task RemoveUnneededProcessorsAfterIdleClientTimeout(int clientCount)
        {
            // arrange
            _hostTransport.Settings.ServerSideClientIdleTimeout = TimeSpan.FromSeconds(1);

            for (int i = 0; i < clientCount; ++i)
            {
                Clients.Add(CreateClient());
                Clients[i].AddContract<IContract>();
            }

            // act & assert
            await StartHost();
            for (int i = 0; i < clientCount; ++i)
            {
                Clients[i].Connect();
                await Clients[i].Get<IContract>().Operation(new Request1());
            }

            using (new AssertionScope())
            {
                Host.GetConnectedClients().Count().Should().Be(clientCount);
                // make clients idle for > allowed idle timeout
                await Task.Delay(_hostTransport.Settings.ServerSideClientIdleTimeout * 2);
                Host.GetConnectedClients().Count().Should().Be(0);
            }
        }
    }
}
