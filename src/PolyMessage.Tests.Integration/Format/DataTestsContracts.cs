using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace PolyMessage.Tests.Integration.Format
{
    [PolyContract]
    public interface IDataContract
    {
        [PolyRequestResponse] Task<EmptyResponse> Empty(EmptyRequest request);
        [PolyRequestResponse] Task<LargeStringResponse> LargeString(LargeStringRequest request);
        [PolyRequestResponse] Task<LargeNumberOfObjectsResponse> LargeNumberOfObjects(LargeNumberOfObjectsRequest request);
        [PolyRequestResponse] Task<LargeArrayResponse> LargeArray(LargeArrayRequest request);
    }

    public sealed class DataImplementor : IDataContract
    {
        public LargeStringRequest LastLargeStringRequest { get; private set; }
        public LargeNumberOfObjectsRequest LastLargeNumberOfObjectsRequest { get; private set; }
        public LargeArrayRequest LastLargeArrayRequest { get; private set; }

        public Task<EmptyResponse> Empty(EmptyRequest request)
        {
            return Task.FromResult(new EmptyResponse());
        }

        public Task<LargeStringResponse> LargeString(LargeStringRequest request)
        {
            LastLargeStringRequest = request;
            return Task.FromResult(new LargeStringResponse());
        }

        public Task<LargeNumberOfObjectsResponse> LargeNumberOfObjects(LargeNumberOfObjectsRequest request)
        {
            LastLargeNumberOfObjectsRequest = request;
            return Task.FromResult(new LargeNumberOfObjectsResponse());
        }

        public Task<LargeArrayResponse> LargeArray(LargeArrayRequest request)
        {
            LastLargeArrayRequest = request;
            return Task.FromResult(new LargeArrayResponse());
        }
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class EmptyRequest {}

    [PolyMessage, Serializable, DataContract]
    public sealed class EmptyResponse {}

    [PolyMessage, Serializable, DataContract]
    public sealed class LargeStringRequest
    {
        [DataMember(Order = 1)] public string LargeString { get; set; }
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class LargeStringResponse
    {}

    [Serializable, DataContract]
    public sealed class Object
    {
        [DataMember(Order = 1)] public string Data { get; set; }
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class LargeNumberOfObjectsRequest
    {
        [DataMember(Order = 1)] public List<Object> Objects { get; set; } = new List<Object>();
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class LargeNumberOfObjectsResponse
    {}

    [PolyMessage, Serializable, DataContract]
    public sealed class LargeArrayRequest
    {
        [DataMember(Order = 1)] public byte[] LargeArray { get; set; }
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class LargeArrayResponse
    {}
}
