using System;
using PolyMessage.Formats.DotNetBinary;
using PolyMessage.Formats.MessagePack;
using PolyMessage.Formats.NewtonsoftJson;
using PolyMessage.Formats.ProtobufNet;
using PolyMessage.Formats.Utf8Json;
using PolyMessage.Transports.Tcp;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Format
{
    public class Data_Tcp_DotNetBinary : DataTests
    {
        public Data_Tcp_DotNetBinary(ITestOutputHelper output) : base(output) {}
        protected override PolyFormat CreateFormat() => new DotNetBinaryFormat();
        protected override PolyTransport CreateTransport(Uri serverAddress) => new TcpTransport(serverAddress, LoggerFactory);
    }

    public class Data_Tcp_MessagePack : DataTests
    {
        public Data_Tcp_MessagePack(ITestOutputHelper output) : base(output) {}
        protected override PolyFormat CreateFormat() => new MessagePackFormat();
        protected override PolyTransport CreateTransport(Uri serverAddress) => new TcpTransport(serverAddress, LoggerFactory);
    }

    public class Data_Tcp_ProtobufNet : DataTests
    {
        public Data_Tcp_ProtobufNet(ITestOutputHelper output) : base(output) {}
        protected override PolyFormat CreateFormat() => new ProtobufNetFormat();
        protected override PolyTransport CreateTransport(Uri serverAddress) => new TcpTransport(serverAddress, LoggerFactory);
    }

    public class Data_Tcp_NewtonsoftJson : DataTests
    {
        public Data_Tcp_NewtonsoftJson(ITestOutputHelper output) : base(output) {}
        protected override PolyFormat CreateFormat() => new NewtonsoftJsonFormat();
        protected override PolyTransport CreateTransport(Uri serverAddress) => new TcpTransport(serverAddress, LoggerFactory);
    }

    public class Data_Tcp_Utf8Json : DataTests
    {
        public Data_Tcp_Utf8Json(ITestOutputHelper output) : base(output) {}
        protected override PolyFormat CreateFormat() => new Utf8JsonFormat();
        protected override PolyTransport CreateTransport(Uri serverAddress) => new TcpTransport(serverAddress, LoggerFactory);
    }
}
