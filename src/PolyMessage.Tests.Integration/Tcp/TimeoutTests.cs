using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using PolyMessage.Tcp;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Tcp
{
    public class TimeoutTests : BaseIntegrationFixture
    {
        private readonly TimeSpan _timeout;
        private readonly TcpTransport _hostTransport;

        public TimeoutTests(ITestOutputHelper output) : base(output, services =>
        {
            services.AddScoped<IContract, Implementor>();
        })
        {
            _timeout = TimeSpan.FromSeconds(1);
            _hostTransport = HostTransport as TcpTransport;

            Client = CreateClient();

            Host.AddContract<IContract>();
            Client.AddContract<IContract>();
        }

        [Fact]
        public async Task ClientShouldThrowWhenBeingIdleMoreThanServerSideTimeout()
        {
            // arrange
            _hostTransport.Settings.ServerSideClientIdleTimeout = _timeout;

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
                act.Should().Throw<PolyConnectionException>();
                Client.Connection.State.Should().Be(PolyConnectionState.Closed);
            }
        }
    }
}
