using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using PolyMessage.Formats.DotNetBinary;
using PolyMessage.Transports.Tcp;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Connection
{
    public class State_Tcp : StateTests
    {
        public State_Tcp(ITestOutputHelper output) : base(output) {}
        protected override PolyFormat CreateFormat() => new DotNetBinaryFormat();
        protected override PolyTransport CreateTransport(Uri serverAddress) => new TcpTransport(serverAddress, LoggerFactory);
    }

    public abstract class StateTests : IntegrationFixture
    {
        protected StateTests(ITestOutputHelper output) : base(output, services =>
        {
            services.AddScoped<IContract, Implementor>();
        })
        {
            Client.AddContract<IContract>();
            Host.AddContract<IContract>();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldSetClientConnectionState(bool hasConnectedToServer)
        {
            // arrange
            await StartHost();

            // act & assert
            using (new AssertionScope())
            {
                Client.Connection.State.Should().Be(PolyConnectionState.Created);

                if (hasConnectedToServer)
                {
                    await Client.ConnectAsync();
                    await Client.Get<IContract>().Operation(new Request1());
                    Client.Connection.State.Should().Be(PolyConnectionState.Opened);
                }

                Client.Disconnect();
                Client.Connection.State.Should().Be(PolyConnectionState.Closed);
            }
        }

        [Fact]
        public async Task ShouldSetServerConnectionState()
        {
            // arrange

            // act & assert
            await StartHostAndConnectClient();
            await Client.Get<IContract>().Operation(new Request1());

            using (new AssertionScope())
            {
                PolyChannel serverClients = Host.GetConnectedClients().First();
                serverClients.Connection.State.Should().Be(PolyConnectionState.Opened);

                serverClients.Close();
                serverClients.Connection.State.Should().Be(PolyConnectionState.Closed);
            }
        }
    }
}
