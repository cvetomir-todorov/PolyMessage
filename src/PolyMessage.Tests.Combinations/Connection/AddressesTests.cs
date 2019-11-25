using PolyMessage.Formats.DotNetBinary;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Combinations.Connection
{
    namespace AddressesTests
    {
        public class Tcp : Integration.Connection.AddressesTests
        {
            public Tcp(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new DotNetBinaryFormat();
            protected override PolyFormat CreateClientFormat() => new DotNetBinaryFormat();
        }
    }
}
