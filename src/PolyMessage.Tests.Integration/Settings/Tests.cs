using System;
using System.Linq;
using System.Net;
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

        private void VerifyAddress(Uri actualAddress, Uri expectedAddress, bool samePorts)
        {
            actualAddress.Scheme.Should().Be(expectedAddress.Scheme);

            IPAddress actualIP = IPAddress.Parse(actualAddress.Host).MapToIPv6();
            IPAddress expectedIP = IPAddress.Parse(expectedAddress.Host).MapToIPv6();
            actualIP.Should().Be(expectedIP);

            if (samePorts)
            {
                actualAddress.Port.Should().Be(expectedAddress.Port);
            }
            else
            {
                actualAddress.Port.Should().NotBe(expectedAddress.Port);
            }
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
                VerifyAddress(localClient.LocalAddress, expectedAddress: ServerAddress, samePorts: false);
                VerifyAddress(localClient.RemoteAddress, expectedAddress: serverSide.LocalAddress, samePorts: true);

                VerifyAddress(serverSide.LocalAddress, expectedAddress: ServerAddress, samePorts: true);
                VerifyAddress(serverSide.RemoteAddress, expectedAddress: localClient.LocalAddress, samePorts: true);
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
