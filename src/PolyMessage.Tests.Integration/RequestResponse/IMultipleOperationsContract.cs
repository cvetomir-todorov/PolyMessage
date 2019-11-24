using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace PolyMessage.Tests.Integration.RequestResponse
{
    [PolyContract]
    public interface IMultipleOperationsContract
    {
        [PolyRequestResponse]
        Task<MultipleOperationsResponse1> Operation1(MultipleOperationsRequest1 request);

        [PolyRequestResponse]
        Task<MultipleOperationsResponse2> Operation2(MultipleOperationsRequest2 request);

        [PolyRequestResponse]
        Task<MultipleOperationsResponse3> Operation3(MultipleOperationsRequest3 request);
    }

    public sealed class MultipleOperationsImplementor : IMultipleOperationsContract
    {
        public Task<MultipleOperationsResponse1> Operation1(MultipleOperationsRequest1 request)
        {
            return Task.FromResult(new MultipleOperationsResponse1{Data = "response1"});
        }

        public Task<MultipleOperationsResponse2> Operation2(MultipleOperationsRequest2 request)
        {
            return Task.FromResult(new MultipleOperationsResponse2 {Data = "response2"});
        }

        public Task<MultipleOperationsResponse3> Operation3(MultipleOperationsRequest3 request)
        {
            return Task.FromResult(new MultipleOperationsResponse3 {Data = "response3"});
        }
    }

    [Serializable]
    [DataContract]
    [PolyMessage]
    public sealed class MultipleOperationsRequest1
    {
        [DataMember(Order = 1)]
        public string Data { get; set; }
    }

    [Serializable]
    [DataContract]
    [PolyMessage]
    public sealed class MultipleOperationsResponse1
    {
        [DataMember(Order = 1)]
        public string Data { get; set; }
    }

    [Serializable]
    [DataContract]
    [PolyMessage]
    public sealed class MultipleOperationsRequest2
    {
        [DataMember(Order = 1)]
        public string Data { get; set; }
    }

    [Serializable]
    [DataContract]
    [PolyMessage]
    public sealed class MultipleOperationsResponse2
    {
        [DataMember(Order = 1)]
        public string Data { get; set; }
    }

    [Serializable]
    [DataContract]
    [PolyMessage]
    public sealed class MultipleOperationsRequest3
    {
        [DataMember(Order = 1)]
        public string Data { get; set; }
    }

    [Serializable]
    [DataContract]
    [PolyMessage]
    public sealed class MultipleOperationsResponse3
    {
        [DataMember(Order = 1)]
        public string Data { get; set; }
    }

}
