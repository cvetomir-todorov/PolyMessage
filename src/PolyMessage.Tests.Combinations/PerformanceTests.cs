using PolyMessage.Formats.DotNetBinary;
using PolyMessage.Formats.MessagePack;
using PolyMessage.Formats.NewtonsoftJson;
using PolyMessage.Formats.ProtobufNet;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Combinations
{
    namespace Performance
    {
        public class Tcp_DotNetBinary : Integration.Performance.PerformanceTests
        {
            public Tcp_DotNetBinary(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new DotNetBinaryFormat();
            protected override PolyFormat CreateClientFormat() => new DotNetBinaryFormat();
        }

        public class Tcp_MessagePack : Integration.Performance.PerformanceTests
        {
            public Tcp_MessagePack(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new MessagePackFormat();
            protected override PolyFormat CreateClientFormat() => new MessagePackFormat();
        }

        public class Tcp_ProtobufNet : Integration.Performance.PerformanceTests
        {
            public Tcp_ProtobufNet(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new ProtobufNetFormat();
            protected override PolyFormat CreateClientFormat() => new ProtobufNetFormat();
        }

        public class Tcp_NewtonsoftJson : Integration.Performance.PerformanceTests
        {
            public Tcp_NewtonsoftJson(ITestOutputHelper output) : base(output)
            {}
            protected override PolyFormat CreateHostFormat() => new NewtonsoftJsonFormat();
            protected override PolyFormat CreateClientFormat() => new NewtonsoftJsonFormat();
        }
    }
}
