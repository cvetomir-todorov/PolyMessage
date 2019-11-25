using PolyMessage.Formats.DotNetBinary;
using PolyMessage.Formats.MessagePack;
using PolyMessage.Formats.ProtobufNet;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Combinations.Format
{
    namespace TypeTests
    {
        public class Tcp_DotNetBinary : Integration.Format.TypeTests
        {
            public Tcp_DotNetBinary(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new DotNetBinaryFormat();
            protected override PolyFormat CreateClientFormat() => new DotNetBinaryFormat();
        }

        public class Tcp_MessagePack : Integration.Format.TypeTests
        {
            public Tcp_MessagePack(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new MessagePackFormat();
            protected override PolyFormat CreateClientFormat() => new MessagePackFormat();
        }

        public class Tcp_ProtobufNet : Integration.Format.TypeTests
        {
            public Tcp_ProtobufNet(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new ProtobufNetFormat();
            protected override PolyFormat CreateClientFormat() => new ProtobufNetFormat();
        }
    }
}
