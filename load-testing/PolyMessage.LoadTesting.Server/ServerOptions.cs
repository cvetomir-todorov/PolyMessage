using System.Collections.Generic;
using System.Security.Authentication;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;

namespace PolyMessage.LoadTesting.Server
{
    public enum Format
    {
        Unknown, DotNetBinary, NewtonsoftJson, MessagePack, ProtobufNet, Utf8Json
    }

    public enum Transport
    {
        Unknown, Tcp
    }

    public interface ITcpOptions
    {
        [Option('s', "tls", SetName = "tcp", Required = false, Default = SslProtocols.None)]
        SslProtocols TcpTlsProtocol { get; set; }
    }

    public sealed class ServerOptions : ITcpOptions
    {
        [Option('l', "logLevel", Default = LogLevel.Information)]
        public LogLevel LogLevel { get; set; }

        [Option('a', "listenAddress", Required = true)]
        public string ListenAddress { get; set; }

        [Option('f', "format", Required = true)]
        public Format Format { get; set; }

        [Option('t', "transport", Required = true)]
        public Transport Transport { get; set; }

        // TCP options
        public SslProtocols TcpTlsProtocol { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>
                {
                    new Example("Start a TCP server using NewtonsoftJson format",
                        new ServerOptions
                        {
                            Transport = Transport.Tcp, ListenAddress = "tcp://192.168.0.101:10678",
                            Format = Format.NewtonsoftJson
                        }),
                    new Example("Start a TLS over TCP server using MessagePack format",
                        new ServerOptions
                        {
                            Transport = Transport.Tcp, TcpTlsProtocol = SslProtocols.Tls12, ListenAddress = "tcp://192.168.0.101:10678",
                            Format = Format.MessagePack
                            
                        })
                };
            }
        }
    }
}
