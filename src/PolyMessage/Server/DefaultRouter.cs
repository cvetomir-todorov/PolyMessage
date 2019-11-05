using System;
using System.Collections.Generic;
using PolyMessage.Endpoints;

namespace PolyMessage.Server
{
    internal interface IRouter
    {
        void BuildRoutingTable(IEnumerable<Endpoint> endpoints);

        Endpoint ChooseEndpoint(object message, IMessageMetadata messageMetadata);
    }

    internal sealed class DefaultRouter : IRouter
    {
        private readonly Dictionary<int, Endpoint> _routingTable;

        public DefaultRouter()
        {
            _routingTable = new Dictionary<int, Endpoint>();
        }

        public void BuildRoutingTable(IEnumerable<Endpoint> endpoints)
        {
            if (endpoints == null)
                throw new ArgumentNullException(nameof(endpoints));
            if (_routingTable.Count > 0)
                throw new InvalidOperationException("Routing table is already built.");

            foreach (Endpoint endpoint in endpoints)
            {
                _routingTable.Add(endpoint.RequestID, endpoint);
            }

            if (_routingTable.Count <= 0)
                throw new ArgumentException("No endpoints were provided.", nameof(endpoints));
        }

        public Endpoint ChooseEndpoint(object message, IMessageMetadata messageMetadata)
        {
            int messageID = messageMetadata.GetMessageID(message.GetType());
            return _routingTable[messageID];
        }
    }
}
