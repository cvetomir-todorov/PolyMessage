using System;

namespace PolyMessage.Messaging
{
    [Serializable]
    internal sealed class PolyHeader
    {
        public int MessageID { get; set; }
    }
}
