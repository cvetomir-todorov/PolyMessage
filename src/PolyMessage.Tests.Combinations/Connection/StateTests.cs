using PolyMessage.Formats.Binary;
using PolyMessage.Formats.MessagePack;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Combinations.Connection
{
    namespace StateTests
    {
        public class Tcp_Binary : Integration.Connection.StateTests
        {
            public Tcp_Binary(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new BinaryFormat();
            protected override PolyFormat CreateClientFormat() => new BinaryFormat();
        }

        public class Tcp_MessagePack : Integration.Connection.StateTests
        {
            public Tcp_MessagePack(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new MessagePackFormat();
            protected override PolyFormat CreateClientFormat() => new MessagePackFormat();
        }
    }
}
