using PolyMessage.Formats.DotNetBinary;
using PolyMessage.Formats.MessagePack;
using PolyMessage.Formats.NewtonsoftJson;
using PolyMessage.Formats.ProtobufNet;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Performance
{
    public class Performance_Tcp_DotNetBinary : PerformanceTests
    {
        public Performance_Tcp_DotNetBinary(ITestOutputHelper output) : base(output)
        {}
        protected override PolyFormat CreateHostFormat() => new DotNetBinaryFormat();
        protected override PolyFormat CreateClientFormat() => new DotNetBinaryFormat();
    }

    public class Performance_Tcp_MessagePack : PerformanceTests
    {
        public Performance_Tcp_MessagePack(ITestOutputHelper output) : base(output)
        {}
        protected override PolyFormat CreateHostFormat() => new MessagePackFormat();
        protected override PolyFormat CreateClientFormat() => new MessagePackFormat();
    }

    public class Performance_Tcp_ProtobufNet : PerformanceTests
    {
        public Performance_Tcp_ProtobufNet(ITestOutputHelper output) : base(output)
        {}
        protected override PolyFormat CreateHostFormat() => new ProtobufNetFormat();
        protected override PolyFormat CreateClientFormat() => new ProtobufNetFormat();
    }

    public class Performance_Tcp_NewtonsoftJson : PerformanceTests
    {
        public Performance_Tcp_NewtonsoftJson(ITestOutputHelper output) : base(output)
        {}
        protected override PolyFormat CreateHostFormat() => new NewtonsoftJsonFormat();
        protected override PolyFormat CreateClientFormat() => new NewtonsoftJsonFormat();
    }
}
