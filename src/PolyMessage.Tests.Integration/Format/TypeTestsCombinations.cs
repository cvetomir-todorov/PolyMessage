using PolyMessage.Formats.DotNetBinary;
using PolyMessage.Formats.MessagePack;
using PolyMessage.Formats.NewtonsoftJson;
using PolyMessage.Formats.ProtobufNet;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Format
{
    public class Type_Tcp_DotNetBinary : TypeTests
    {
        public Type_Tcp_DotNetBinary(ITestOutputHelper output) : base(output)
        {}
        protected override PolyFormat CreateHostFormat() => new DotNetBinaryFormat();
        protected override PolyFormat CreateClientFormat() => new DotNetBinaryFormat();
    }

    public class Type_Tcp_MessagePack : TypeTests
    {
        public Type_Tcp_MessagePack(ITestOutputHelper output) : base(output)
        {}
        protected override PolyFormat CreateHostFormat() => new MessagePackFormat();
        protected override PolyFormat CreateClientFormat() => new MessagePackFormat();
    }

    public class Type_Tcp_ProtobufNet : TypeTests
    {
        public Type_Tcp_ProtobufNet(ITestOutputHelper output) : base(output)
        {}
        protected override PolyFormat CreateHostFormat() => new ProtobufNetFormat();
        protected override PolyFormat CreateClientFormat() => new ProtobufNetFormat();
    }

    public class Type_Tcp_NewtonsoftJson : TypeTests
    {
        public Type_Tcp_NewtonsoftJson(ITestOutputHelper output) : base(output)
        {}
        protected override PolyFormat CreateHostFormat() => new NewtonsoftJsonFormat();
        protected override PolyFormat CreateClientFormat() => new NewtonsoftJsonFormat();
    }
}
