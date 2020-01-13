using System;
using PolyMessage.Formats.DotNetBinary;
using PolyMessage.Formats.MessagePack;
using PolyMessage.Formats.NewtonsoftJson;
using PolyMessage.Formats.ProtobufNet;
using PolyMessage.Formats.Utf8Json;
using PolyMessage.Transports.Tcp;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.RequestResponse
{
    public class RequestResponse_Tcp_DotNetBinary : RequestResponseTests
    {
        public RequestResponse_Tcp_DotNetBinary(ITestOutputHelper output) : base(output) {}
        protected override PolyFormat CreateFormat() => new DotNetBinaryFormat();
        protected override PolyTransport CreateTransport(Uri serverAddress) => new TcpTransport(serverAddress, LoggerFactory);
    }

    public class RequestResponse_Tcp_MessagePack : RequestResponseTests
    {
        public RequestResponse_Tcp_MessagePack(ITestOutputHelper output) : base(output) {}
        protected override PolyFormat CreateFormat() => new MessagePackFormat();
        protected override PolyTransport CreateTransport(Uri serverAddress) => new TcpTransport(serverAddress, LoggerFactory);
    }

    public class RequestResponse_Tcp_ProtobufNet : RequestResponseTests
    {
        public RequestResponse_Tcp_ProtobufNet(ITestOutputHelper output) : base(output) {}
        protected override PolyFormat CreateFormat() => new ProtobufNetFormat();
        protected override PolyTransport CreateTransport(Uri serverAddress) => new TcpTransport(serverAddress, LoggerFactory);
    }

    public class RequestResponse_Tcp_NewtonsoftJson : RequestResponseTests
    {
        public RequestResponse_Tcp_NewtonsoftJson(ITestOutputHelper output) : base(output) {}
        protected override PolyFormat CreateFormat() => new NewtonsoftJsonFormat();
        protected override PolyTransport CreateTransport(Uri serverAddress) => new TcpTransport(serverAddress, LoggerFactory);
    }

    public class RequestResponse_Tcp_Utf8Json : RequestResponseTests
    {
        public RequestResponse_Tcp_Utf8Json(ITestOutputHelper output) : base(output) {}
        protected override PolyFormat CreateFormat() => new Utf8JsonFormat();
        protected override PolyTransport CreateTransport(Uri serverAddress) => new TcpTransport(serverAddress, LoggerFactory);
    }
}
