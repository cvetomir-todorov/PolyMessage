using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace PolyMessage.Tests.Integration.RequestResponse
{
    [PolyContract]
    public interface ISingleOperationContract
    {
        [PolyRequestResponse]
        Task<SingleOperationResponse> Operation(SingleOperationRequest request);
    }

    public sealed class SingleOperationImplementor : ISingleOperationContract
    {
        public Task<SingleOperationResponse> Operation(SingleOperationRequest request)
        {
            return Task.FromResult(new SingleOperationResponse { Data = "response" });
        }
    }

    [Serializable]
    [DataContract]
    [PolyMessage]
    public sealed class SingleOperationRequest
    {
        [DataMember(Order = 1)]
        public string Data { get; set; }
    }

    [Serializable]
    [DataContract]
    [PolyMessage]
    public sealed class SingleOperationResponse
    {
        [DataMember(Order = 1)]
        public string Data { get; set; }
    }
}