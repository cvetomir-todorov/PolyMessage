using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ProtoBuf;

namespace PolyMessage.Formats.ProtobufNet
{
    public class ProtobufNetFormat : PolyFormat
    {
        private readonly ConcurrentDictionary<int, Type> _fieldNumberTypeMap;
        private readonly ConcurrentDictionary<Type, int> _typeFieldNumberMap;
        private int _fieldNumber;

        public ProtobufNetFormat()
        {
            _fieldNumberTypeMap = new ConcurrentDictionary<int, Type>();
            _typeFieldNumberMap = new ConcurrentDictionary<Type, int>();
        }

        public override string DisplayName => "ProtobufNet";

        public override void RegisterMessageTypes(IEnumerable<Type> messageTypes)
        {
            foreach (Type messageType in messageTypes)
            {
                Serializer.NonGeneric.PrepareSerializer(messageType);
                int fieldNumber = Interlocked.Increment(ref _fieldNumber);
                _fieldNumberTypeMap.TryAdd(fieldNumber, messageType);
                _typeFieldNumberMap.TryAdd(messageType, fieldNumber);
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

        internal Serializer.TypeResolver TypeResolver => GetMessageType;

        public override PolyFormatter CreateFormatter(PolyChannel channel)
        {
            return new ProtobufNetFormatter(this, channel);
        }
    }
}
