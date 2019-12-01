using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace PolyMessage.LoadTesting.Contracts
{
    [PolyMessage(ID = 1), DataContract]
    public sealed class EmptyRequest
    {}

    [PolyMessage(ID = 2), DataContract]
    public sealed class EmptyResponse
    {}

    public interface ILoadTestingContract : IPolyContract
    {
        [PolyRequestResponse]
        Task<EmptyResponse> EmptyOperation(EmptyRequest request);
    }
}
