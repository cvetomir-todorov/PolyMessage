using PolyMessage.Formats.DotNetBinary;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Combinations.Connection
{
    namespace StateTests
    {
        public class Tcp : Integration.Connection.StateTests
        {
            public Tcp(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new DotNetBinaryFormat();
            protected override PolyFormat CreateClientFormat() => new DotNetBinaryFormat();
        }
    }
}
