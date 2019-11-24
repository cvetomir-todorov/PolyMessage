using PolyMessage.Formats.Binary;
using PolyMessage.Formats.MessagePack;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Combinations
{
    namespace TimeoutTests
    {
        public class Tcp_Binary : Integration.Tcp.TimeoutTests
        {
            public Tcp_Binary(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new BinaryFormat();
            protected override PolyFormat CreateClientFormat() => new BinaryFormat();
        }

        public class Tcp_MessagePack : Integration.Tcp.TimeoutTests
        {
            public Tcp_MessagePack(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new MessagePackFormat();
            protected override PolyFormat CreateClientFormat() => new MessagePackFormat();
        }
    }
}
