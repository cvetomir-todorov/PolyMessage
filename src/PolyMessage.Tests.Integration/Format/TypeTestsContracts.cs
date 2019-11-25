using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace PolyMessage.Tests.Integration.Format
{
    [PolyContract]
    public interface ITypeContract
    {
        [PolyRequestResponse] Task<SimpleTypesResponse> SimpleTypes(SimpleTypesRequest request);
        [PolyRequestResponse] Task<ValueTypesResponse> ValueTypes(ValueTypesRequest request);
        [PolyRequestResponse] Task<ClassesResponse> Classes(ClassesRequest request);
        [PolyRequestResponse] Task<CollectionsResponse> Collections(CollectionsRequest request);
    }

    public class TypeImplementor : ITypeContract
    {
        public SimpleTypesRequest LastSimpleTypesRequest { get; private set; }
        public ValueTypesRequest LastValueTypesRequest { get; private set; }
        public ClassesRequest LastClassesRequest { get; private set; }
        public CollectionsRequest LastCollectionsRequest { get; private set; }

        public Task<SimpleTypesResponse> SimpleTypes(SimpleTypesRequest request)
        {
            LastSimpleTypesRequest = request;
            return Task.FromResult(new SimpleTypesResponse());
        }

        public Task<ValueTypesResponse> ValueTypes(ValueTypesRequest request)
        {
            LastValueTypesRequest = request;
            return Task.FromResult(new ValueTypesResponse());
        }

        public Task<ClassesResponse> Classes(ClassesRequest request)
        {
            LastClassesRequest = request;
            return Task.FromResult(new ClassesResponse());
        }

        public Task<CollectionsResponse> Collections(CollectionsRequest request)
        {
            LastCollectionsRequest = request;
            return Task.FromResult(new CollectionsResponse());
        }
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class SimpleTypesRequest
    {
        // signed
        [DataMember(Order = 1)] public sbyte SByte { get; set; }
        [DataMember(Order = 2)] public Int16 Int16 { get; set; }
        [DataMember(Order = 3)] public Int32 Int32 { get; set; }
        [DataMember(Order = 4)] public Int64 Int64 { get; set; }
        // floating point
        [DataMember(Order = 5)] public Single Single { get; set; }
        [DataMember(Order = 6)] public Double Double { get; set; }
        [DataMember(Order = 7)] public Decimal Decimal { get; set; }
        // unsigned
        [DataMember(Order = 8)] public byte Byte { get; set; }
        [DataMember(Order = 9)] public UInt16 UInt16 { get; set; }
        [DataMember(Order = 10)] public UInt32 UInt32 { get; set; }
        [DataMember(Order = 11)] public UInt64 UInt64 { get; set; }
        // others
        [DataMember(Order = 12)] public bool Bool { get; set; }
        [DataMember(Order = 13)] public char Char { get; set; }
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class SimpleTypesResponse
    {}

    [Serializable, DataContract]
    public enum CustomEnum
    {
        Default, Normal, Extreme = int.MaxValue
    }

    [Serializable, DataContract]
    public struct CustomStruct
    {
        [DataMember(Order = 1)] public int ID { get; set; }
        [DataMember(Order = 2)] public string Data { get; set; }
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class ValueTypesRequest
    {
        [DataMember(Order = 1)] public CustomEnum Enum { get; set; }
        [DataMember(Order = 2)] public CustomStruct Struct { get; set; }
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class ValueTypesResponse
    {}

    [Serializable, DataContract]
    public sealed class CustomClassRoot
    {
        [DataMember(Order = 1)] public CustomClassMiddle Middle1 { get; set; }
        [DataMember(Order = 2)] public CustomClassMiddle Middle2 { get; set; }
    }

    [Serializable, DataContract]
    public sealed class CustomClassMiddle
    {
        [DataMember(Order = 1)] public CustomClassLeaf Leaf { get; set; }
    }

    [Serializable, DataContract]
    public sealed class CustomClassLeaf
    {
        [DataMember(Order = 1)] public int ID { get; set; }
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class ClassesRequest
    {
        [DataMember(Order = 1)] public CustomClassRoot Root { get; set; }
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class ClassesResponse
    {}

    [Serializable, DataContract]
    public sealed class ClassWithID
    {
        public ClassWithID() { }
        public ClassWithID(int id) => ID = id;
        [DataMember(Order = 1)] public int ID { get; set; }
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class CollectionsRequest
    {
        [DataMember(Order = 1)] public List<int> List { get; set; } = new List<int>();
        [DataMember(Order = 2)] public Dictionary<int, ClassWithID> Dictionary { get; set; } = new Dictionary<int, ClassWithID>();
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class CollectionsResponse
    {}
}
