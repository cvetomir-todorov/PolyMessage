using System;
using System.Collections.Generic;

namespace PolyMessage.Metadata
{
    public interface IMessageMetadata
    {
        Type GetMessageType(short messageTypeID);

        short GetMessageTypeID(Type messageType);
    }

    internal class MessageMetadata : IMessageMetadata
    {
        private readonly Dictionary<short, Type> _idTypeMap;
        private readonly Dictionary<Type, short> _typeIDMap;

        internal MessageMetadata(Dictionary<short, Type> idTypeMap, Dictionary<Type, short> typeIDMap)
        {
            if (idTypeMap == null)
                throw new ArgumentNullException(nameof(idTypeMap));
            if (typeIDMap == null)
                throw new ArgumentNullException(nameof(typeIDMap));
            if (idTypeMap.Count <= 0)
                throw new ArgumentException("ID<->type map should not be empty.", nameof(idTypeMap));
            if (typeIDMap.Count <= 0)
                throw new ArgumentException("Type<->ID map should not be empty.", nameof(typeIDMap));
            if (idTypeMap.Count != typeIDMap.Count)
                throw new ArgumentException("Both maps should have the same size.");

            _idTypeMap = idTypeMap;
            _typeIDMap = typeIDMap;
        }

        private void EnsureBuilt()
        {
            if (_idTypeMap.Count <= 0 || _typeIDMap.Count <= 0)
                throw new InvalidOperationException("Metadata has not been built.");
        }

        public Type GetMessageType(short messageTypeID)
        {
            EnsureBuilt();
            if (!_idTypeMap.TryGetValue(messageTypeID, out Type messageType))
            {
                throw new InvalidOperationException($"Missing metadata for message with type ID {messageTypeID}.");
            }

            return messageType;
        }

        public short GetMessageTypeID(Type messageType)
        {
            EnsureBuilt();
            if (!_typeIDMap.TryGetValue(messageType, out short messageTypeID))
            {
                throw new InvalidOperationException($"Missing metadata for message type {messageType.Name}.");
            }

            return messageTypeID;
        }
    }
}
