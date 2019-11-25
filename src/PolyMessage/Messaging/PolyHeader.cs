using System;
using System.Runtime.Serialization;

namespace PolyMessage.Messaging
{
    /// <summary>
    /// Represents the header for the protocol used.
    /// </summary>
    [Serializable]
    [DataContract]
    public sealed class PolyHeader
    {
        [DataMember(Order = 1)]
        public int MessageID { get; set; }
    }
}