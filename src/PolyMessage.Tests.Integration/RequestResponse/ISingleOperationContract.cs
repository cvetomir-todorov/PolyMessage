using System;
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
    [PolyMessage]
    public sealed class SingleOperationRequest
    {
        public string Data { get; set; }
    }

    [Serializable]
    [PolyMessage]
    public sealed class SingleOperationResponse
    {
        public string Data { get; set; }
    }
}