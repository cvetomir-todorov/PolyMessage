using System;
using System.Collections.Generic;
using ProtoBuf;

namespace PolyMessage.Formats.ProtobufNet
{
    public class ProtobufNetFormat : PolyFormat
    {
        public override string DisplayName => "ProtobufNet";

        public override void RegisterMessageTypes(IEnumerable<MessageInfo> messageTypes)
        {
            if (messageTypes == null)
                throw new ArgumentNullException(nameof(messageTypes));

            foreach (MessageInfo messageInfo in messageTypes)
            {
                Serializer.NonGeneric.PrepareSerializer(messageInfo.Type);
            }
        }

        public override PolyFormatter CreateFormatter()
        {
            return new ProtobufNetFormatter(this);
        }
    }
}
