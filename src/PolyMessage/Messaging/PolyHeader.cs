using System;
using System.Runtime.Serialization;

namespace PolyMessage.Messaging
{
    /// <summary>
    /// Represents the header for the protocol used.
    /// </summary>
    [PolyMessage(ID = TypeID)]
    [Serializable]
    [DataContract]
    public sealed class PolyHeader
    {
        public const short TypeID = 1;

        [DataMember(Order = 1)]
        public short MessageTypeID { get; set; }
    }
}