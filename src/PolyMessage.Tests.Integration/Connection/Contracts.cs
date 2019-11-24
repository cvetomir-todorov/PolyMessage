using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace PolyMessage.Tests.Integration.Connection
{
    public sealed class Implementor : IContract
    {
        public Task<Response1> Operation(Request1 request)
        {
            return Task.FromResult(new Response1());
        }
    }

    [PolyContract]
    public interface IContract
    {
        [PolyRequestResponse]
        Task<Response1> Operation(Request1 request);
    }

    [Serializable]
    [DataContract]
    [PolyMessage]
    public sealed class Request1
    {}

    [Serializable]
    [DataContract]
    [PolyMessage]
    public sealed class Response1
    {}
}
