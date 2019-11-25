using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace PolyMessage.Tests.Integration.Performance
{
    [PolyContract]
    public interface IPerformanceContract
    {
        [PolyRequestResponse] Task<PerformanceResponse1> Operation1(PerformanceRequest1 request);
        [PolyRequestResponse] Task<PerformanceResponse2> Operation2(PerformanceRequest2 request);
        [PolyRequestResponse] Task<PerformanceResponse3> Operation3(PerformanceRequest3 request);
    }

    public sealed class PerformanceImplementor : IPerformanceContract
    {
        public Task<PerformanceResponse1> Operation1(PerformanceRequest1 request)
        {
            return Task.FromResult(new PerformanceResponse1 {Data = "response1"});
        }

        public Task<PerformanceResponse2> Operation2(PerformanceRequest2 request)
        {
            return Task.FromResult(new PerformanceResponse2 {Data = "response2"});
        }

        public Task<PerformanceResponse3> Operation3(PerformanceRequest3 request)
        {
            return Task.FromResult(new PerformanceResponse3 {Data = "response3"});
        }
    }

    [Serializable, DataContract, PolyMessage]
    public sealed class PerformanceRequest1
    {
        [DataMember(Order = 1)] public string Data { get; set; }
    }

    [Serializable, DataContract, PolyMessage]
    public sealed class PerformanceResponse1
    {
        [DataMember(Order = 1)] public string Data { get; set; }
    }

    [Serializable, DataContract, PolyMessage]
    public sealed class PerformanceRequest2
    {
        [DataMember(Order = 1)] public string Data { get; set; }
    }

    [Serializable, DataContract, PolyMessage]
    public sealed class PerformanceResponse2
    {
        [DataMember(Order = 1)] public string Data { get; set; }
    }

    [Serializable, DataContract, PolyMessage]
    public sealed class PerformanceRequest3
    {
        [DataMember(Order = 1)] public string Data { get; set; }
    }

    [Serializable, DataContract, PolyMessage]
    public sealed class PerformanceResponse3
    {
        [DataMember(Order = 1)] public string Data { get; set; }
    }
}
