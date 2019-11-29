using PolyMessage.Formats.DotNetBinary;
using PolyMessage.Formats.MessagePack;
using PolyMessage.Formats.NewtonsoftJson;
using PolyMessage.Formats.ProtobufNet;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.RequestResponse
{
    public class RequestResponse_Tcp_DotNetBinary : RequestResponseTests
    {
        public RequestResponse_Tcp_DotNetBinary(ITestOutputHelper output) : base(output)
        {}
        protected override PolyFormat CreateHostFormat() => new DotNetBinaryFormat();
        protected override PolyFormat CreateClientFormat() => new DotNetBinaryFormat();
    }

    public class RequestResponse_Tcp_MessagePack : RequestResponseTests
    {
        public RequestResponse_Tcp_MessagePack(ITestOutputHelper output) : base(output)
        {}
        protected override PolyFormat CreateHostFormat() => new MessagePackFormat();
        protected override PolyFormat CreateClientFormat() => new MessagePackFormat();
    }

    public class RequestResponse_Tcp_ProtobufNet : RequestResponseTests
    {
        public RequestResponse_Tcp_ProtobufNet(ITestOutputHelper output) : base(output)
        {}
        protected override PolyFormat CreateHostFormat() => new ProtobufNetFormat();
        protected override PolyFormat CreateClientFormat() => new ProtobufNetFormat();
    }

    public class RequestResponse_Tcp_NewtonsoftJson : RequestResponseTests
    {
        public RequestResponse_Tcp_NewtonsoftJson(ITestOutputHelper output) : base(output)
        {}
        protected override PolyFormat CreateHostFormat() => new NewtonsoftJsonFormat();
        protected override PolyFormat CreateClientFormat() => new NewtonsoftJsonFormat();
    }
}
