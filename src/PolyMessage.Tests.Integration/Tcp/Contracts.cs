using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace PolyMessage.Tests.Integration.Tcp
{
    public sealed class Implementor : IContract
    {
        public Task<Response1> Operation(Request1 request)
        {
            return Task.FromResult(new Response1 {Data = "response"});
        }
    }

    [PolyContract]
    public interface IContract
    {
        [PolyRequestResponse]
        Task<Response1> Operation(Request1 request);
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class Request1
    {
        [DataMember(Order = 1)] public string Data { get; set; }
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class Response1
    {
        [DataMember(Order = 1)] public string Data { get; set; }
    }
}
