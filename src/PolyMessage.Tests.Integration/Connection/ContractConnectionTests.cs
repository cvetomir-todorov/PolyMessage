using System;
using System.Net;
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
    public class ContractConnection_Tcp : ContractConnectionTests
    {
        public ContractConnection_Tcp(ITestOutputHelper output) : base(output, TransportUnderTest.Tcp) {}
        protected override PolyFormat CreateFormat() => new DotNetBinaryFormat();
        protected override PolyTransport CreateTransport(Uri address) => new TcpTransport(address, LoggerFactory);
    }

    public abstract class ContractConnectionTests : IntegrationFixture
    {
        protected ContractConnectionTests(ITestOutputHelper output, TransportUnderTest transport) : base(output, transport, services =>
        {
            services.AddScoped<IContractWithConnection, ImplementorWithConnection>();
        })
        {
            Host.AddContract<IContractWithConnection>();
            Client.AddContract<IContractWithConnection>();
        }

        private void VerifyAddress(Uri actualAddress, Uri expectedAddress, bool samePorts = true)
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
        public async Task SetConnectionInImplementor()
        {
            // arrange

            // act
            await StartHostAndConnectClient();
            GetConnectionResponse response = await Client.Get<IContractWithConnection>().GetConnection(new GetConnectionRequest());

            // assert
            using (new AssertionScope())
            {
                response.State.Should().Be(PolyConnectionState.Opened);
                VerifyAddress(response.LocalAddress, ServerAddress);
                VerifyAddress(response.RemoteAddress, Client.Connection.LocalAddress);
            }
        }

        [Fact]
        public async Task SetConnectionInProxy()
        {
            // arrange

            // act
            await StartHostAndConnectClient();
            IContractWithConnection proxy = Client.Get<IContractWithConnection>();

            // assert
            using (new AssertionScope())
            {
                proxy.Connection.Should().NotBeNull();
                if (proxy.Connection != null)
                {
                    proxy.Connection.State.Should().Be(PolyConnectionState.Opened);
                    VerifyAddress(proxy.Connection.RemoteAddress, ServerAddress);
                    VerifyAddress(proxy.Connection.LocalAddress, ServerAddress, samePorts: false);
                }
            }
        }
    }
}
