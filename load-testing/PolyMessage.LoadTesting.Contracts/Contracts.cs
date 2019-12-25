using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace PolyMessage.LoadTesting.Contracts
{
    [PolyMessage(ID = 1024), DataContract]
    public sealed class EmptyRequest
    {}

    [PolyMessage(ID = 1025), DataContract]
    public sealed class EmptyResponse
    {}

    public interface ILoadTestingContract : IPolyContract
    {
        [PolyRequestResponse]
        Task<EmptyResponse> EmptyOperation(EmptyRequest request);
    }
}
