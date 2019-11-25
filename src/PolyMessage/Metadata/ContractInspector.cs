using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace PolyMessage.Metadata
{
    internal interface IContractInspector
    {
        IEnumerable<Operation> InspectContract(Type contractType);
    }

    internal sealed class ContractInspector : IContractInspector
    {
        private readonly ILogger _logger;

        public ContractInspector(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(GetType());
        }

        public IEnumerable<Operation> InspectContract(Type contractType)
        {
            MethodInfo[] methods = contractType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            _logger.LogDebug("Contract {0} has {1} operations.", contractType.Name, methods.Length);

            foreach (MethodInfo method in methods)
            {
                // methods have a single parameter representing the request message
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

                _logger.LogDebug("{0}", operation);
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
