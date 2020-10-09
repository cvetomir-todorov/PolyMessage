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
    public class Type_Tcp_DotNetBinary : TypeTests
    {
        public Type_Tcp_DotNetBinary(ITestOutputHelper output) : base(output, TransportUnderTest.Tcp) {}
        protected override PolyFormat CreateFormat() => new DotNetBinaryFormat();
        protected override PolyTransport CreateTransport(Uri address) => new TcpTransport(address, LoggerFactory);
    }

    public class Type_Tcp_MessagePack : TypeTests
    {
        public Type_Tcp_MessagePack(ITestOutputHelper output) : base(output, TransportUnderTest.Tcp) {}
        protected override PolyFormat CreateFormat() => new MessagePackFormat();
        protected override PolyTransport CreateTransport(Uri address) => new TcpTransport(address, LoggerFactory);
    }

    public class Type_Tcp_ProtobufNet : TypeTests
    {
        public Type_Tcp_ProtobufNet(ITestOutputHelper output) : base(output, TransportUnderTest.Tcp) {}
        protected override PolyFormat CreateFormat() => new ProtobufNetFormat();
        protected override PolyTransport CreateTransport(Uri address) => new TcpTransport(address, LoggerFactory);
    }

    public class Type_Tcp_NewtonsoftJson : TypeTests
    {
        public Type_Tcp_NewtonsoftJson(ITestOutputHelper output) : base(output, TransportUnderTest.Tcp) {}
        protected override PolyFormat CreateFormat() => new NewtonsoftJsonFormat();
        protected override PolyTransport CreateTransport(Uri address) => new TcpTransport(address, LoggerFactory);
    }

    public class Type_Tcp_Utf8Json : TypeTests
    {
        public Type_Tcp_Utf8Json(ITestOutputHelper output) : base(output, TransportUnderTest.Tcp) {}
        protected override PolyFormat CreateFormat() => new Utf8JsonFormat();
        protected override PolyTransport CreateTransport(Uri address) => new TcpTransport(address, LoggerFactory);
    }
}
