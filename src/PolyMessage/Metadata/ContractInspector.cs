using System;
using System.Collections.Generic;
using System.Reflection;

namespace PolyMessage.Metadata
{
    internal interface IContractInspector
    {
        IEnumerable<Operation> InspectContract(Type contractType);
    }

    // FEAT: validate contracts, operations, attributes and their values
    internal sealed class ContractInspector : IContractInspector
    {
        public IEnumerable<Operation> InspectContract(Type contractType)
        {
            MethodInfo[] methods = contractType.GetMethods(BindingFlags.Instance | BindingFlags.Public);

            foreach (MethodInfo method in methods)
            {
                //PolyRequestResponseAttribute operationAttribute = method.GetCustomAttribute<PolyRequestResponseAttribute>();
                ParameterInfo requestParameter = method.GetParameters()[0];
                Type requestType = requestParameter.ParameterType;
                // methods return Task<T> where T is the response message type
                Type responseType = method.ReturnType.GenericTypeArguments[0];

                Operation operation = new Operation();

                operation.RequestID = GetMessageID(requestType);
                operation.RequestType = requestType;
                operation.ResponseID = GetMessageID(responseType);
                operation.ResponseType = responseType;
                operation.Method = method;
                operation.ContractType = contractType;

                yield return operation;
            }
        }

        private static int GetMessageID(Type messageType)
        {
            PolyMessageAttribute messageAttribute = messageType.GetCustomAttribute<PolyMessageAttribute>();
            if (messageAttribute == null)
            {
                throw new InvalidOperationException($"Message {messageType.Name} should have {typeof(PolyMessageAttribute).Name}.");
            }

            int messageID = messageAttribute.ID;
            if (messageID == 0)
            {
                messageID = messageType.GetHashCode();
            }

            return messageID;
        }
    }
}
