using PolyMessage.Formats.DotNetBinary;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Combinations.Tcp
{
    namespace SecurityTests
    {
        public class Tcp : Integration.Tcp.SecurityTests
        {
            public Tcp(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new DotNetBinaryFormat();
            protected override PolyFormat CreateClientFormat() => new DotNetBinaryFormat();
        }
    }
}
