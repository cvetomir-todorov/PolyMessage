using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace PolyMessage.Tests.Integration.Connection
{
    [PolyMessage, Serializable, DataContract]
    public sealed class Request1 {}

    [PolyMessage, Serializable, DataContract]
    public sealed class Response1 {}

    [PolyContract]
    public interface IContract
    {
        [PolyRequestResponse]
        Task<Response1> Operation(Request1 request);
    }

    public sealed class Implementor : IContract
    {
        public Task<Response1> Operation(Request1 request)
        {
            return Task.FromResult(new Response1());
        }
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class GetConnectionRequest {}

    [PolyMessage, Serializable, DataContract]
    public sealed class GetConnectionResponse
    {
        [DataMember(Order = 1)] public PolyConnectionState State { get; set; }
        [DataMember(Order = 2)] public Uri LocalAddress { get; set; }
        [DataMember(Order = 3)] public Uri RemoteAddress { get; set; }
    }

    [PolyContract]
    public interface IContractWithConnection : IPolyContract
    {
        [PolyRequestResponse]
        Task<GetConnectionResponse> GetConnection(GetConnectionRequest request);
    }

    public sealed class ImplementorWithConnection : IContractWithConnection
    {
        public PolyConnection Connection { get; set; }

        public Task<GetConnectionResponse> GetConnection(GetConnectionRequest request)
        {
            return Task.FromResult(new GetConnectionResponse
            {
                State = Connection.State,
                LocalAddress = Connection.LocalAddress,
                RemoteAddress = Connection.RemoteAddress
            });
        }
    }
}
