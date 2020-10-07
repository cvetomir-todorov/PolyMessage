using System;
using System.Collections.Generic;

namespace PolyMessage.Metadata
{
    internal interface IMessageMetadataBuilder
    {
        IMessageMetadata Build(IEnumerable<Operation> operations);
    }

    internal class MessageMetadataBuilder : IMessageMetadataBuilder
    {
        public IMessageMetadata Build(IEnumerable<Operation> operations)
        {
            if (operations == null)
                throw new ArgumentNullException(nameof(operations));

            Dictionary<short, Type> idTypeMap = new Dictionary<short, Type>();
            Dictionary<Type, short> typeIDMap = new Dictionary<Type, short>();

            foreach (Operation operation in operations)
            {
                idTypeMap.Add(operation.RequestTypeID, operation.RequestType);
                typeIDMap.Add(operation.RequestType, operation.RequestTypeID);

                idTypeMap.Add(operation.ResponseTypeID, operation.ResponseType);
                typeIDMap.Add(operation.ResponseType, operation.ResponseTypeID);
            }

            if (idTypeMap.Count <= 0 || typeIDMap.Count <= 0)
                throw new ArgumentException("No operations were provided.", nameof(operations));

            return new MessageMetadata(idTypeMap, typeIDMap);
        }
    }
}
