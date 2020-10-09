using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PolyMessage.Formats.DotNetBinary;
using PolyMessage.Transports.Tcp;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.ImplementorProvision
{
    public class DisposeTests : IntegrationFixture
    {
        public DisposeTests(ITestOutputHelper output) : base(output, TransportUnderTest.Tcp, services =>
        {
            services.AddScoped<IDisposableContract, DisposableImplementor>();
        })
        {
            Host.AddContract<IDisposableContract>();
            Client.AddContract<IDisposableContract>();
        }

        protected override PolyFormat CreateFormat() => new DotNetBinaryFormat();
        protected override PolyTransport CreateTransport(Uri address) => new TcpTransport(address, LoggerFactory);

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async Task DisposeImplementorEachCall(int callCount)
        {
            // arrange
            DisposableImplementor.ResetDisposedCount();

            // act
            await StartHostAndConnectClient();
            IDisposableContract contract = Client.Get<IDisposableContract>();
            for (int i = 0; i < callCount; ++i)
            {
                if (i % 2 == 0)
                    await contract.Operation1(new Request1());
                else
                    await contract.Operation2(new Request2());
            }

            // assert
            DisposableImplementor.DisposedCount.Should().Be(callCount);
        }
    }
}
