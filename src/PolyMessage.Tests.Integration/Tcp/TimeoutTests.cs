using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using PolyMessage.Transports.Tcp;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Tcp
{
    public abstract class TimeoutTests : IntegrationFixture
    {
        private readonly TimeSpan _timeout;
        private readonly TcpTransport _hostTransport;

        protected TimeoutTests(ITestOutputHelper output) : base(output, services =>
        {
            services.AddScoped<IContract, Implementor>();
        })
        {
            _timeout = TimeSpan.FromSeconds(1);
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
            _hostTransport.Settings.ServerSideClientIdleTimeout = _timeout;

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

        [Fact]
        public async Task ClientShouldThrowWhenBeingIdleMoreThanServerSideTimeout()
        {
            // arrange
            _hostTransport.Settings.ServerSideClientIdleTimeout = _timeout;

            Client = CreateClient();
            Client.AddContract<IContract>();

            // act
            await StartHost();
            Client.Connect();
            IContract contract = Client.Get<IContract>();
            await contract.Operation(new Request1());

            await Task.Delay(_timeout * 2);
            Func<Task> act = async () => await contract.Operation(new Request1());

            // assert
            using (new AssertionScope())
            {
                act.Should().Throw<PolyConnectionClosedException>().Which.CloseReason.Should().Be(PolyConnectionCloseReason.RemoteAbortedConnection);
                Client.Connection.State.Should().Be(PolyConnectionState.Closed);
            }
        }
    }
}
