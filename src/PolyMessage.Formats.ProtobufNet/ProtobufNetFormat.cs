using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ProtoBuf;

namespace PolyMessage.Formats.ProtobufNet
{
    public class ProtobufNetFormat : PolyFormat
    {
        private readonly ConcurrentDictionary<int, Type> _fieldNumberTypeMap;
        private readonly ConcurrentDictionary<Type, int> _typeFieldNumberMap;

        public ProtobufNetFormat()
        {
            _fieldNumberTypeMap = new ConcurrentDictionary<int, Type>();
            _typeFieldNumberMap = new ConcurrentDictionary<Type, int>();
        }

        public override string DisplayName => "ProtobufNet";

        public override void RegisterMessageTypes(IEnumerable<MessageInfo> messageTypes)
        {
            foreach (MessageInfo messageInfo in messageTypes)
            {
                Serializer.NonGeneric.PrepareSerializer(messageInfo.Type);
                _fieldNumberTypeMap.TryAdd(messageInfo.TypeID, messageInfo.Type);
                _typeFieldNumberMap.TryAdd(messageInfo.Type, messageInfo.TypeID);
            }
        }

        internal int GetFieldNumber(Type messageType)
        {
            if (!_typeFieldNumberMap.TryGetValue(messageType, out int fieldNumber))
            {
                throw new PolyFormatException(PolyFormatError.TypeRegistration, $"Type {messageType} was not registered.", this);
            }
            return fieldNumber;
        }

        internal Type GetMessageType(int fieldNumber)
        {
            if (!_fieldNumberTypeMap.TryGetValue(fieldNumber, out Type messageType))
            {
                throw new PolyFormatException(PolyFormatError.TypeRegistration, $"Type for field number {fieldNumber} was not registered.", this);
            }
            return messageType;
        }

        public override PolyFormatter CreateFormatter(PolyChannel channel)
        {
            return new ProtobufNetFormatter(this, channel);
        }
    }
}
