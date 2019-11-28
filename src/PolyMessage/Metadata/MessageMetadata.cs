using System;
using System.Collections.Generic;

namespace PolyMessage.Metadata
{
    internal interface IMessageMetadata
    {
        void Build(IEnumerable<Operation> operations);

        Type GetMessageType(int messageTypeID);

        int GetMessageTypeID(Type messageType);
    }

    internal class MessageMetadata : IMessageMetadata
    {
        private readonly Dictionary<int, Type> _idTypeMap;
        private readonly Dictionary<Type, int> _typeIDMap;

        public MessageMetadata()
        {
            _idTypeMap = new Dictionary<int, Type>();
            _typeIDMap = new Dictionary<Type, int>();
        }

        public void Build(IEnumerable<Operation> operations)
        {
            if (operations == null)
                throw new ArgumentNullException(nameof(operations));
            if (_idTypeMap.Count > 0 || _typeIDMap.Count > 0)
                throw new InvalidOperationException("Metadata is already built.");

            foreach (Operation operation in operations)
            {
                AddMetadata(operation.RequestTypeID, operation.RequestType);
                AddMetadata(operation.ResponseTypeID, operation.ResponseType);
            }

            if (_idTypeMap.Count <= 0 || _typeIDMap.Count <= 0)
                throw new ArgumentException("No operations were provided.", nameof(operations));
        }

        private void AddMetadata(int messageTypeID, Type messageType)
        {
            _idTypeMap.Add(messageTypeID, messageType);
            _typeIDMap.Add(messageType, messageTypeID);
        }

        private void EnsureBuilt()
        {
            if (_idTypeMap.Count <= 0 || _typeIDMap.Count <= 0)
                throw new InvalidOperationException("Metadata has not been built.");
        }

        public Type GetMessageType(int messageTypeID)
        {
            EnsureBuilt();
            if (!_idTypeMap.TryGetValue(messageTypeID, out Type messageType))
            {
                throw new InvalidOperationException($"Missing metadata for message with type ID {messageTypeID}.");
            }

            return messageType;
        }

        public int GetMessageTypeID(Type messageType)
        {
            EnsureBuilt();
            if (!_typeIDMap.TryGetValue(messageType, out int messageTypeID))
            {
                throw new InvalidOperationException($"Missing metadata for message type {messageType.Name}.");
            }

            return messageTypeID;
        }
    }
}
