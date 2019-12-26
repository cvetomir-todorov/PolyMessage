using System.Collections.Generic;
using System.IO;
using ProtoBuf;

namespace PolyMessage.Formats.ProtobufNet
{
    public class ProtobufNetFormat : PolyFormat
    {
        public override string DisplayName => "ProtobufNet";

        public override void RegisterMessageTypes(IEnumerable<MessageInfo> messageTypes)
        {
            foreach (MessageInfo messageInfo in messageTypes)
            {
                Serializer.NonGeneric.PrepareSerializer(messageInfo.Type);
            }
        }

        public override PolyFormatter CreateFormatter(Stream stream)
        {
            return new ProtobufNetFormatter(this, stream);
        }
    }
}
