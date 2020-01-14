using System;
using System.IO;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyMessage.Formats.DotNetBinary;
using PolyMessage.Formats.MessagePack;
using PolyMessage.Formats.NewtonsoftJson;
using PolyMessage.Formats.ProtobufNet;
using PolyMessage.Formats.Utf8Json;
using PolyMessage.Transports.Tcp;

namespace PolyMessage.LoadTesting.Server
{
    public sealed class ServerFactory
    {
        private readonly ILogger _logger;
        private readonly ServerOptions _options;

        public ServerFactory(ILogger logger, ServerOptions options)
        {
            _logger = logger;
            _options = options;
        }

        public PolyFormat CreateFormat()
        {
            switch (_options.Format)
            {
                case Format.DotNetBinary:
                    return new DotNetBinaryFormat();
                case Format.NewtonsoftJson:
                    return new NewtonsoftJsonFormat();
                case Format.MessagePack:
                    return new MessagePackFormat();
                case Format.ProtobufNet:
                    return new ProtobufNetFormat();
                case Format.Utf8Json:
                    return new Utf8JsonFormat();
                default:
                {
                    _logger.LogError("Format '{0}' is invalid.", _options.Format);
                    Environment.Exit(1);
                    return null;
                }
            }
        }

        public PolyTransport CreateTransport(IServiceProvider serviceProvider)
        {
            switch (_options.Transport)
            {
                case Transport.Tcp:
                    return CreateTcpTransport(serviceProvider);
                default:
                {
                    _logger.LogError("Transport '{0}' is invalid.", _options.Transport);
                    Environment.Exit(2);
                    return null;
                }
            }
        }

        private PolyTransport CreateTcpTransport(IServiceProvider serviceProvider)
        {
            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            TcpTransport transport = new TcpTransport(new Uri(_options.ListenAddress), loggerFactory);
            transport.Settings.ReceiveBufferSize = 65536;
            transport.Settings.SendBufferSize = 65536;
            transport.MessageBufferSettings.InitialSize = 65536;

            if (_options.TcpTlsProtocol != SslProtocols.None)
            {
                transport.Settings.TlsProtocol = _options.TcpTlsProtocol;
                transport.Settings.TlsServerCertificate = LoadTlsCertificate();
            }

            return transport;
        }

        private X509Certificate2 LoadTlsCertificate()
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDirectory = Path.GetDirectoryName(assemblyPath);
            string certificatePath = Path.Combine(assemblyDirectory, "Certificates/PolyMessage.Tests.Server.pfx");

            if (!File.Exists(certificatePath))
            {
                _logger.LogError("TLS certificate file does not exist - {0}", certificatePath);
                Environment.Exit(3);
            }

            X509Certificate2 tlsCertificate = new X509Certificate2(certificatePath, "t3st");
            return tlsCertificate;
        }
    }
}
