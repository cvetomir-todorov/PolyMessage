using System.Collections.Generic;
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

    [PolyMessage(ID = 1026), DataContract]
    public sealed class StringRequest
    {
        [DataMember(Order = 1)] public string Data { get; set; }
    }

    [PolyMessage(ID = 1027), DataContract]
    public sealed class StringResponse
    {
        [DataMember(Order = 1)] public string Data { get; set; }
    }

    [DataContract]
    public sealed class Dto
    {
        [DataMember(Order = 1)] public int ID { get; set; }
    }

    [PolyMessage(ID = 1028), DataContract]
    public sealed class ObjectsRequest
    {
        [DataMember(Order = 1)] public List<Dto> Objects { get; set; } = new List<Dto>();
    }

    [PolyMessage(ID = 1029), DataContract]
    public sealed class ObjectsResponse
    {
        [DataMember(Order = 1)] public List<Dto> Objects { get; set; } = new List<Dto>();
    }

    public interface ILoadTestingContract : IPolyContract
    {
        [PolyRequestResponse]
        Task<EmptyResponse> EmptyOperation(EmptyRequest request);

        [PolyRequestResponse]
        Task<StringResponse> StringOperation(StringRequest request);

        [PolyRequestResponse]
        Task<ObjectsResponse> ObjectsOperation(ObjectsRequest request);
    }
}
