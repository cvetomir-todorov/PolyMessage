using System;
using System.Collections.Generic;

namespace PolyMessage.Endpoints
{
    internal interface IMessageMetadata
    {
        void Build(IEnumerable<Endpoint> endpoints);

        Type GetMessageType(int messageID);

        int GetMessageID(Type messageType);
    }

    internal class DefaultMessageMetadata : IMessageMetadata
    {
        private readonly Dictionary<int, Type> _idTypeMap;
        private readonly Dictionary<Type, int> _typeIDMap;

        public DefaultMessageMetadata()
        {
            _idTypeMap = new Dictionary<int, Type>();
            _typeIDMap = new Dictionary<Type, int>();
        }

        public void Build(IEnumerable<Endpoint> endpoints)
        {
            if (endpoints == null)
                throw new ArgumentNullException(nameof(endpoints));
            if (_idTypeMap.Count > 0 || _typeIDMap.Count > 0)
                throw new InvalidOperationException("Metadata is already built.");

            foreach (Endpoint endpoint in endpoints)
            {
                AddMetadata(endpoint.RequestID, endpoint.RequestType);
                AddMetadata(endpoint.ResponseID, endpoint.ResponseType);
            }

            if (_idTypeMap.Count <= 0 || _typeIDMap.Count <= 0)
                throw new ArgumentException("No endpoints were provided.", nameof(endpoints));
        }

        private void AddMetadata(int messageID, Type messageType)
        {
            _idTypeMap.Add(messageID, messageType);
            _typeIDMap.Add(messageType, messageID);
        }

        private void EnsureBuilt()
        {
            if (_idTypeMap.Count <= 0 || _typeIDMap.Count <= 0)
                throw new InvalidOperationException("");
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
