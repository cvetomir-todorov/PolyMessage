using PolyMessage.Formats.DotNetBinary;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Combinations.Tcp
{
    namespace TimeoutTests
    {
        public class Tcp : Integration.Tcp.TimeoutTests
        {
            public Tcp(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new DotNetBinaryFormat();
            protected override PolyFormat CreateClientFormat() => new DotNetBinaryFormat();
        }
    }
}
