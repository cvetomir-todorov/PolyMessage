using System;
using System.Collections.Generic;
using PolyMessage.Metadata;

namespace PolyMessage.Server
{
    internal interface IRouter
    {
        void BuildRoutingTable(IEnumerable<Operation> operations);

        Operation ChooseOperation(object message, IMessageMetadata messageMetadata);
    }

    internal sealed class Router : IRouter
    {
        private readonly Dictionary<short, Operation> _routingTable;

        public Router()
        {
            _routingTable = new Dictionary<short, Operation>();
        }

        public void BuildRoutingTable(IEnumerable<Operation> operations)
        {
            if (operations == null)
                throw new ArgumentNullException(nameof(operations));
            if (_routingTable.Count > 0)
                throw new InvalidOperationException("Routing table is already built.");

            foreach (Operation operation in operations)
            {
                _routingTable.Add(operation.RequestTypeID, operation);
            }

            if (_routingTable.Count <= 0)
                throw new ArgumentException("No operations were provided.", nameof(operations));
        }

        public Operation ChooseOperation(object message, IMessageMetadata messageMetadata)
        {
            short messageTypeID = messageMetadata.GetMessageTypeID(message.GetType());
            return _routingTable[messageTypeID];
        }
    }
}
