using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Settings
{
    public class Tests : BaseIntegrationFixture
    {
        public Tests(ITestOutputHelper output) : base(output, services =>
        {
            services.AddScoped<IContract, Implementor>();
        })
        {}

        private PolyClient Setup()
        {
            PolyClient client = CreateClient(ServerAddress, ServiceProvider);
            Clients.Add(client);

            Host.AddContract<IContract>();
            client.AddContract<IContract>();

            return client;
        }

        private async Task Connect(PolyClient client)
        {
            await StartHost();
            client.Connect();
            await client.Get<IContract>().Operation(new Request1());
        }

        [Fact]
        public async Task ExposeLocalAndRemoteAddresses()
        {
            // arrange & act
            PolyClient localClient = Setup();
            await Connect(localClient);
            PolyChannel serverSide = Host.GetConnectedClients().First();

            // assert
            using (new AssertionScope())
            {
                localClient.LocalAddress.Scheme.Should().Be(ServerAddress.Scheme);
                localClient.LocalAddress.Host.Should().Be(ServerAddress.Host);
                localClient.LocalAddress.Port.Should().NotBe(ServerAddress.Port);
                localClient.RemoteAddress.Should().Be(ServerAddress);

                serverSide.LocalAddress.Should().Be(ServerAddress);
                serverSide.RemoteAddress.Scheme.Should().Be(ServerAddress.Scheme);
                serverSide.RemoteAddress.Host.Should().Be(ServerAddress.Host);
                serverSide.RemoteAddress.Port.Should().NotBe(ServerAddress.Port);

                localClient.LocalAddress.Should().Be(serverSide.RemoteAddress);
            }
        }
    }

    public sealed class Implementor : IContract
    {
        public Task<Response1> Operation(Request1 request)
        {
            return Task.FromResult(new Response1());
        }
    }

    [PolyContract]
    public interface IContract
    {
        [PolyRequestResponse]
        Task<Response1> Operation(Request1 request);
    }

    [Serializable]
    [PolyMessage]
    public sealed class Request1
    {}

    [Serializable]
    [PolyMessage]
    public sealed class Response1
    {}
}
