using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Connection
{
    public class StateTests : BaseIntegrationFixture
    {
        private readonly PolyClient _client;

        public StateTests(ITestOutputHelper output) : base(output, services =>
        {
            services.AddScoped<IContract, Implementor>();
        })
        {
            _client = CreateClient(ServerAddress, ServiceProvider);
            Clients.Add(_client);

            _client.AddContract<IContract>();
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
                _client.Connection.State.Should().Be(PolyConnectionState.Created);

                if (hasConnectedToServer)
                {
                    _client.Connect();
                    await _client.Get<IContract>().Operation(new Request1());
                    _client.Connection.State.Should().Be(PolyConnectionState.Opened);
                }

                _client.Dispose();
                _client.Connection.State.Should().Be(PolyConnectionState.Closed);
            }
        }

        [Fact]
        public async Task ShouldSetServerConnectionState()
        {
            // arrange
            await StartHost();

            // act & assert
            using (new AssertionScope())
            {
                _client.Connect();
                await _client.Get<IContract>().Operation(new Request1());

                PolyChannel remoteClient = Host.GetConnectedClients().First();
                remoteClient.Connection.State.Should().Be(PolyConnectionState.Opened);

                remoteClient.Dispose();
                remoteClient.Connection.State.Should().Be(PolyConnectionState.Closed);
            }
        }
    }
}
