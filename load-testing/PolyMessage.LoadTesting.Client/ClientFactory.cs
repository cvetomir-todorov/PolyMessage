using System;
using System.Net.Security;
using System.Security.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyMessage.Formats.DotNetBinary;
using PolyMessage.Formats.MessagePack;
using PolyMessage.Formats.NewtonsoftJson;
using PolyMessage.Formats.ProtobufNet;
using PolyMessage.Formats.Utf8Json;
using PolyMessage.Transports.Tcp;

namespace PolyMessage.LoadTesting.Client
{
    public sealed class ClientFactory
    {
        private readonly ILogger _logger;
        private readonly ClientOptions _options;

        public ClientFactory(ILogger logger, ClientOptions options)
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
            TcpTransport transport = new TcpTransport(new Uri(_options.ServerAddress), loggerFactory);
            transport.Settings.ReceiveBufferSize = 65536;
            transport.Settings.SendBufferSize = 65536;
            transport.MessageBufferSettings.InitialSize = 65536;

            if (_options.TcpTlsProtocol != SslProtocols.None)
            {
                transport.Settings.TlsProtocol = _options.TcpTlsProtocol;
                transport.Settings.TlsClientRemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                {
                    if (errors != SslPolicyErrors.None)
                    {
                        _logger.LogDebug("Validating self-signed certificate with the following errors: {0}", errors);
                    }
                    return true;
                };
            }

            return transport;
        }
    }
}
