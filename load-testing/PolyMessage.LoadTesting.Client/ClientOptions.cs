using System.Collections.Generic;
using System.Security.Authentication;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;

namespace PolyMessage.LoadTesting.Client
{
    public enum Format
    {
        Unknown, DotNetBinary, NewtonsoftJson, MessagePack, ProtobufNet
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

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>
                {
                    // TODO: generate examples
                    //new Example("Start a TCP server using NewtonsoftJson format",
                    //    new ServerOptions
                    //    {
                    //        ListenAddress = "tcp://192.168.0.101:10678", Format = Format.NewtonsoftJson, Transport = Transport.Tcp
                    //    }),
                    //new Example("Start a TLS over TCP server using MessagePack format",
                    //    new ServerOptions
                    //    {
                    //        ListenAddress = "tcp://192.168.0.101:10678", Format = Format.MessagePack, Transport = Transport.Tcp,
                    //        TcpTlsProtocol = SslProtocols.Tls12
                    //    })
                };
            }
        }
    }
}
