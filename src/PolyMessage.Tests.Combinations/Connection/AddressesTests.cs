using PolyMessage.Formats.DotNetBinary;
using PolyMessage.Formats.MessagePack;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Combinations.Connection
{
    namespace AddressesTests
    {
        public class Tcp_DotNetBinary : Integration.Connection.AddressesTests
        {
            public Tcp_DotNetBinary(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new DotNetBinaryFormat();
            protected override PolyFormat CreateClientFormat() => new DotNetBinaryFormat();
        }

        public class Tcp_MessagePack : Integration.Connection.AddressesTests
        {
            public Tcp_MessagePack(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new MessagePackFormat();
            protected override PolyFormat CreateClientFormat() => new MessagePackFormat();
        }
    }
}
