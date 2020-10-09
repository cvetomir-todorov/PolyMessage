using System;
using System.Runtime.Serialization;

namespace PolyMessage.Transports.Ipc.Messaging
{
    [PolyMessage(ID = TypeID)]
    [Serializable]
    [DataContract]
    public sealed class ProtocolHeader
    {
        public const short TypeID = 1;

        [DataMember(Order = 1)]
        public short MessageTypeID { get; set; }
    }
}
