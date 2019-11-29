using System;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using PolyMessage.Formats.DotNetBinary;
using PolyMessage.Transports.Tcp;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Tcp
{
    public class Security_Tcp : SecurityTests
    {
        public Security_Tcp(ITestOutputHelper output) : base(output) {}
        protected override PolyFormat CreateFormat() => new DotNetBinaryFormat();
        protected override PolyTransport CreateTransport(Uri serverAddress) => new TcpTransport(serverAddress, LoggerFactory);
    }

    public abstract class SecurityTests : IntegrationFixture
    {
        private readonly TcpTransport _hostTransport;
        private readonly TcpTransport _clientTransport;

        protected SecurityTests(ITestOutputHelper output) : base(output, services =>
        {
            services.AddScoped<IContract, Implementor>();
        })
        {
            SslProtocols tlsProtocol = SslProtocols.Tls12;
            X509Certificate2 serverCertificate = LoadServerCertificate();

            _hostTransport = (TcpTransport)HostTransport;
            _hostTransport.Settings.TlsProtocol = tlsProtocol;
            _hostTransport.Settings.TlsServerCertificate = serverCertificate;

            _clientTransport = (TcpTransport) ClientTransport;
            _clientTransport.Settings.TlsProtocol = tlsProtocol;
            _clientTransport.Settings.TlsClientRemoteCertificateValidationCallback =
                (sender, certificate, chain, errors) => certificate.Subject == serverCertificate.Subject;

            Host.AddContract<IContract>();
            Client.AddContract<IContract>();
        }

        private X509Certificate2 LoadServerCertificate()
        {
            string certificatePath = Path.Combine(GetTestDirectory(), "Certificates\\PolyMessage.Tests.Server.pfx");
            X509Certificate2 serverCertificate = new X509Certificate2(certificatePath, "t3st");
            return serverCertificate;
        }

        [Theory]
        [InlineData(SslProtocols.None, SslProtocols.Tls)]
        [InlineData(SslProtocols.None, SslProtocols.Tls11)]
        [InlineData(SslProtocols.None, SslProtocols.Tls12)]
        [InlineData(SslProtocols.None, SslProtocols.Tls13)]
        public void ClientThrowsWhenNoTlsIsConfiguredClientSide(SslProtocols clientProtocol, SslProtocols serverProtocol)
        {
            // arrange
            _clientTransport.Settings.TlsProtocol = clientProtocol;
            _hostTransport.Settings.TlsProtocol = serverProtocol;

            // act
            Func<Task> act = async () =>
            {
                await StartHostAndConnectClient();
                await Client.Get<IContract>().Operation(new Request1 {Data = "request"});
            };

            // assert
            using (new AssertionScope())
            {
                act.Should().Throw<PolyConnectionClosedException>().Which.InnerException.Should().NotBeNull();
            }
        }

        [Theory]
        [InlineData(SslProtocols.Tls, SslProtocols.None)]
        [InlineData(SslProtocols.Tls11, SslProtocols.None)]
        [InlineData(SslProtocols.Tls12, SslProtocols.None)]
        [InlineData(SslProtocols.Tls13, SslProtocols.None)]
        public void ClientThrowsWhenNoTlsIsConfiguredServerSide(SslProtocols clientProtocol, SslProtocols serverProtocol)
        {
            // arrange
            _clientTransport.Settings.TlsProtocol = clientProtocol;
            _hostTransport.Settings.TlsProtocol = serverProtocol;

            // act
            Func<Task> act = async () => await StartHostAndConnectClient();

            // assert
            using (new AssertionScope())
            {
                act.Should().Throw<PolyOpenConnectionException>().Which.InnerException.Should().NotBeNull();
            }
        }

        [Theory]
        [InlineData(SslProtocols.Tls, SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls, SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls, SslProtocols.Tls13)]
        [InlineData(SslProtocols.Tls11, SslProtocols.Tls)]
        [InlineData(SslProtocols.Tls11, SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls11, SslProtocols.Tls13)]
        [InlineData(SslProtocols.Tls12, SslProtocols.Tls)]
        [InlineData(SslProtocols.Tls12, SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls12, SslProtocols.Tls13)]
        [InlineData(SslProtocols.Tls13, SslProtocols.Tls)]
        [InlineData(SslProtocols.Tls13, SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls13, SslProtocols.Tls12)]
        public void ClientThrowsWhenTlsConfigurationMismatch(SslProtocols clientProtocol, SslProtocols serverProtocol)
        {
            // arrange
            _clientTransport.Settings.TlsProtocol = clientProtocol;
            _hostTransport.Settings.TlsProtocol = serverProtocol;

            // act
            Func<Task> act = async () => await StartHostAndConnectClient();

            // assert
            using (new AssertionScope())
            {
                act.Should().Throw<PolyOpenConnectionException>().Which.InnerException.Should().NotBeNull();
            }
        }

        [Fact]
        public void ClientThrowsWhenServerCertificateIsMissing()
        {
            // arrange
            _hostTransport.Settings.TlsServerCertificate = null;
            _clientTransport.Settings.TlsClientRemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true;

            // act
            Func<Task> act = async () => await StartHostAndConnectClient();

            // assert
            act.Should().Throw<PolyOpenConnectionException>().Which.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task UseCertificateInRequestResponse()
        {
            // arrange

            // act
            await StartHostAndConnectClient();
            Response1 response = await Client.Get<IContract>().Operation(new Request1 {Data = "request"});

            // assert
            using (new AssertionScope())
            {
                response.Should().NotBeNull();
                response.Data.Should().NotBeNullOrWhiteSpace();
            }
        }
    }
}
