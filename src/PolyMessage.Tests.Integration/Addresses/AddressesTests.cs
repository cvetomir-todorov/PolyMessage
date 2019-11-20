using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Addresses
{
    public class AddressesTests : BaseIntegrationFixture
    {
        public AddressesTests(ITestOutputHelper output) : base(output, services =>
        {
            services.AddScoped<IContract, Implementor>();
        })
        {}

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
            PolyClient localClient = CreateClient(ServerAddress, ServiceProvider);
            Clients.Add(localClient);

            Host.AddContract<IContract>();
            localClient.AddContract<IContract>();

            await StartHost();
            localClient.Connect();
            await localClient.Get<IContract>().Operation(new Request1());
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
}
