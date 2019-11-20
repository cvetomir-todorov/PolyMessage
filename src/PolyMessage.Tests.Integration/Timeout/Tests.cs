using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using PolyMessage.Tcp;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Timeout
{
    public class Tests : BaseIntegrationFixture
    {
        private readonly TimeSpan _timeout;
        private readonly TcpTransport _hostTransport;
        private readonly PolyClient _client;

        public Tests(ITestOutputHelper output) : base(output, services =>
        {
            services.AddScoped<IContract, Implementor>();
        })
        {
            _timeout = TimeSpan.FromSeconds(1);
            _hostTransport = HostTransport as TcpTransport;

            _client = CreateClient(ServerAddress, ServiceProvider);
            Clients.Add(_client);

            Host.AddContract<IContract>();
            _client.AddContract<IContract>();
        }

        private async Task Start()
        {
            await StartHost();
            _client.Connect();
        }

        [Fact]
        public async Task ClientShouldThrowWhenBeingIdleMoreThanServerSideTimeout()
        {
            // arrange
            _hostTransport.Settings.ServerSideClientIdleTimeout = _timeout;

            // act
            await Start();
            IContract contract = _client.Get<IContract>();

            await contract.Operation(new Request1());
            await Task.Delay(_timeout * 2);
            Func<Task> act = async () => await contract.Operation(new Request1());

            // assert
            using (new AssertionScope())
            {
                act.Should().Throw<PolyConnectionException>();
                _client.State.Should().Be(CommunicationState.Closed);
            }
        }
    }
}
