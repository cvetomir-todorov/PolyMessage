using System.Collections.Generic;
using System.Security.Authentication;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;

namespace PolyMessage.LoadTesting.Client
{
    public enum Format
    {
        Unknown, DotNetBinary, NewtonsoftJson, MessagePack, ProtobufNet, Utf8Json
    }

    public enum Transport
    {
        Unknown, Tcp
    }

    public enum Messaging
    {
        Unknown, Empty, String, Objects
    }

    public interface ITcpOptions
    {
        [Option('s', "tls", SetName = "tcp", Required = false, Default = SslProtocols.None)]
        SslProtocols TcpTlsProtocol { get; set; }
    }

    public sealed class ClientOptions : ITcpOptions
    {
        [Option('l', "logLevel", Default = LogLevel.Information)]
        public LogLevel LogLevel { get; set; }

        [Option('a', "serverAddress", Required = true)]
        public string ServerAddress { get; set; }

        [Option('f', "format", Required = true)]
        public Format Format { get; set; }

        [Option('t', "transport", Required = true)]
        public Transport Transport { get; set; }

        // TCP options
        public SslProtocols TcpTlsProtocol { get; set; }

        [Option('c', "clients", Required = true)]
        public int Clients { get; set; }

        [Option('n', "transactions", Required = true)]
        public int Transactions { get; set; }

        // Messaging options
        [Option("messaging", Default = Messaging.Empty)]
        public Messaging Messaging { get; set; }

        [Option("messagingStringLength", Default = 128)]
        public int MessagingStringLength { get; set; }

        [Option("messagingObjectsCount", Default = 64)]
        public int MessagingObjectsCount { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>
                {
                    new Example("Start a TCP server using NewtonsoftJson format",
                        new ClientOptions
                        {
                            Transport = Transport.Tcp, ServerAddress = "tcp://192.168.0.101:10678",
                            Format = Format.NewtonsoftJson
                        }),
                    new Example("Start a TLS over TCP server using MessagePack format",
                        new ClientOptions
                        {
                            Transport = Transport.Tcp, TcpTlsProtocol = SslProtocols.Tls12, ServerAddress = "tcp://192.168.0.101:10678",
                            Format = Format.MessagePack
                        })
                };
            }
        }
    }
}
