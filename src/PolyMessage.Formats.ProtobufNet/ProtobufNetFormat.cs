using System;
using System.Collections.Generic;
using ProtoBuf;

namespace PolyMessage.Formats.ProtobufNet
{
    public class ProtobufNetFormat : PolyFormat
    {
        private readonly Dictionary<int, Type> _fieldNumberTypeMap;
        private readonly Dictionary<Type, int> _typeFieldNumberMap;

        public ProtobufNetFormat()
        {
            _fieldNumberTypeMap = new Dictionary<int, Type>();
            _typeFieldNumberMap = new Dictionary<Type, int>();
        }

        public override string DisplayName => "ProtobufNet";

        public override void RegisterMessageTypes(IEnumerable<Type> messageTypes)
        {
            int fieldNumber = 0;

            foreach (Type messageType in messageTypes)
            {
                Serializer.NonGeneric.PrepareSerializer(messageType);
                fieldNumber++;
                _fieldNumberTypeMap.Add(fieldNumber, messageType);
                _typeFieldNumberMap.Add(messageType, fieldNumber);
            }
        }

        internal int GetFieldNumber(Type messageType)
        {
            return _typeFieldNumberMap[messageType];
        }

        internal Type GetMessageType(int fieldNumber)
        {
            return _fieldNumberTypeMap[fieldNumber];
        }

        internal Serializer.TypeResolver TypeResolver => GetMessageType;

        public override PolyFormatter CreateFormatter(PolyChannel channel)
        {
            return new ProtobufNetFormatter(this, channel);
        }
    }
}
