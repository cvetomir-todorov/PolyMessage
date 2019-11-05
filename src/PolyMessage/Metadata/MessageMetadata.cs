using System;
using System.Collections.Generic;

namespace PolyMessage.Metadata
{
    internal interface IMessageMetadata
    {
        void Build(IEnumerable<Operation> operations);

        Type GetMessageType(int messageID);

        int GetMessageID(Type messageType);
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
                AddMetadata(operation.RequestID, operation.RequestType);
                AddMetadata(operation.ResponseID, operation.ResponseType);
            }

            if (_idTypeMap.Count <= 0 || _typeIDMap.Count <= 0)
                throw new ArgumentException("No operations were provided.", nameof(operations));
        }

        private void AddMetadata(int messageID, Type messageType)
        {
            _idTypeMap.Add(messageID, messageType);
            _typeIDMap.Add(messageType, messageID);
        }

        private void EnsureBuilt()
        {
            if (_idTypeMap.Count <= 0 || _typeIDMap.Count <= 0)
                throw new InvalidOperationException("Metadata has not been built.");
        }

        public Type GetMessageType(int messageID)
        {
            EnsureBuilt();
            if (!_idTypeMap.TryGetValue(messageID, out Type messageType))
            {
                throw new InvalidOperationException($"Missing metadata for message with ID {messageID}.");
            }

            return messageType;
        }

        public int GetMessageID(Type messageType)
        {
            EnsureBuilt();
            if (!_typeIDMap.TryGetValue(messageType, out int messageID))
            {
                throw new InvalidOperationException($"Missing metadata for message type {messageType.Name}.");
            }

            return messageID;
        }
    }
}
