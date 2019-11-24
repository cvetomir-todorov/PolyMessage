using PolyMessage.Formats.Binary;
using PolyMessage.Formats.MessagePack;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Combinations
{
    namespace RequestResponseTests
    {
        public class Tcp_Binary : Integration.RequestResponse.RequestResponseTests
        {
            public Tcp_Binary(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new BinaryFormat();
            protected override PolyFormat CreateClientFormat() => new BinaryFormat();
        }

        public class Tcp_MessagePack : Integration.RequestResponse.RequestResponseTests
        {
            public Tcp_MessagePack(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new MessagePackFormat();
            protected override PolyFormat CreateClientFormat() => new MessagePackFormat();
        }
    }
}
