using System;
using System.Threading.Tasks;

namespace PolyMessage.Tests.Integration.Server
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
    [PolyMessage]
    public sealed class Request1
    {}

    [Serializable]
    [PolyMessage]
    public sealed class Response1
    {}
}
