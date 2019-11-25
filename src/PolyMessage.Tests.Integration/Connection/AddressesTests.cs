using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Connection
{
    public abstract class AddressesTests : IntegrationFixture
    {
        protected AddressesTests(ITestOutputHelper output) : base(output, services =>
        {
            services.AddScoped<IContract, Implementor>();
        })
        {
            Host.AddContract<IContract>();
            Client.AddContract<IContract>();
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
        public async Task ExposeClientAddresses()
        {
            // arrange & act
            await StartHostAndConnectClient();
            await Client.Get<IContract>().Operation(new Request1());
            PolyChannel remoteClient = Host.GetConnectedClients().First();

            // assert
            using (new AssertionScope())
            {
                VerifyAddress(Client.Connection.LocalAddress, expectedAddress: ServerAddress, samePorts: false);
                VerifyAddress(Client.Connection.RemoteAddress, expectedAddress: ServerAddress, samePorts: true);
                Client.Connection.RemoteAddress.Port.Should().Be(remoteClient.Connection.LocalAddress.Port);
            }
        }

        [Fact]
        public async Task ExposeServerAddresses()
        {
            // arrange & act
            await StartHostAndConnectClient();
            await Client.Get<IContract>().Operation(new Request1());
            PolyChannel remoteClient = Host.GetConnectedClients().First();

            // assert
            using (new AssertionScope())
            {
                VerifyAddress(remoteClient.Connection.LocalAddress, expectedAddress: ServerAddress, samePorts: true);
                VerifyAddress(remoteClient.Connection.RemoteAddress, expectedAddress: ServerAddress, samePorts: false);
                remoteClient.Connection.RemoteAddress.Port.Should().Be(Client.Connection.LocalAddress.Port);
            }
        }
    }
}
