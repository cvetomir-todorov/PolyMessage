using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
        private static readonly List<Operation> _emptyList = new List<Operation>(capacity: 0);

        public ContractInspector(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(GetType());
        }

        public IEnumerable<Operation> InspectContract(Type contractType)
        {
            List<PolyContractValidationError> errors = null;

            PolyContractAttribute contractAttribute = contractType.GetCustomAttribute<PolyContractAttribute>();
            bool inheritsContractInterface = contractType.GetInterfaces().Any(@interface => @interface == typeof(IPolyContract));
            if (contractAttribute == null && !inheritsContractInterface)
            {
                AddError(ref errors, contractType, $"{contractType.Name} is missing {typeof(PolyContractAttribute).Name} and does not inherit {typeof(IPolyContract).Name}.");
            }

            MethodInfo[] methods = contractType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            List<Operation> operations = _emptyList;
            if (methods.Length == 0)
            {
                AddError(ref errors, contractType, $"{contractType.Name} has 0 operations.");
            }
            else
            {
                operations = InspectOperations(contractType, methods, ref errors);
            }

            if (errors != null && errors.Count > 0)
            {
                throw new PolyContractException(errors);
            }
            else
            {
                _logger.LogDebug("{0} has {1} operations.", contractType.Name, methods.Length);
                foreach (Operation operation in operations)
                {
                    _logger.LogDebug("{0}", operation);
                }

                return operations;
            }
        }

        private static List<Operation> InspectOperations(Type contractType, MethodInfo[] methods, ref List<PolyContractValidationError> errors)
        {
            List<Operation> operations = new List<Operation>(methods.Length);
            Dictionary<int, Type> messageTypeIDs = new Dictionary<int, Type>();
            Dictionary<Type, Operation> messageTypes = new Dictionary<Type, Operation>();

            foreach (MethodInfo method in methods)
            {
                Operation operation = new Operation();
                operation.ContractType = contractType;
                operation.Method = method;

                PolyRequestResponseAttribute requestResponseAttribute = method.GetCustomAttribute<PolyRequestResponseAttribute>();
                if (requestResponseAttribute == null)
                {
                    AddError(ref errors, contractType, $"{contractType.Name}.{method.Name} is missing {typeof(PolyRequestResponseAttribute).Name}.");
                }

                InspectResponse(contractType, method, operation, messageTypeIDs, messageTypes, ref errors);
                InspectRequest(contractType, method, operation, messageTypeIDs, messageTypes, ref errors);

                operations.Add(operation);
            }

            return operations;
        }

        private static void InspectResponse(
            Type contractType,
            MethodInfo method,
            Operation operation,
            Dictionary<int, Type> messageTypeIDs,
            Dictionary<Type, Operation> messageTypes,
            ref List<PolyContractValidationError> errors)
        {
            // operations should return Task<T> where T is the response type
            if (method.ReturnType.BaseType == typeof(Task) && method.ReturnType.GenericTypeArguments.Length == 1)
            {
                Type responseType = method.ReturnType.GenericTypeArguments[0];
                PolyMessageAttribute messageAttribute = responseType.GetCustomAttribute<PolyMessageAttribute>();
                if (messageAttribute == null)
                {
                    AddError(ref errors, contractType, $"{contractType.Name}.{method.Name} {responseType.Name} is missing {typeof(PolyMessageAttribute).Name}.");
                }
                else
                {
                    operation.ResponseTypeID = InspectMessageType(
                        contractType, method, responseType, messageAttribute, operation, messageTypeIDs, messageTypes, ref errors);
                    operation.ResponseType = responseType;
                }
            }
            else
            {
                AddError(ref errors, contractType, $"{contractType.Name}.{method.Name} does not return Task<{method.ReturnType.Name}>.");
            }
        }

        private static void InspectRequest(
            Type contractType,
            MethodInfo method,
            Operation operation,
            Dictionary<int, Type> messageTypeIDs,
            Dictionary<Type, Operation> messageTypes,
            ref List<PolyContractValidationError> errors)
        {
            // methods have a single parameter representing the request message
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 1)
            {
                AddError(ref errors, contractType, $"{contractType.Name}.{method.Name} does not have exactly 1 parameter.");
            }
            else
            {
                ParameterInfo requestParameter = method.GetParameters()[0];
                Type requestType = requestParameter.ParameterType;
                PolyMessageAttribute messageAttribute = requestType.GetCustomAttribute<PolyMessageAttribute>();
                if (messageAttribute == null)
                {
                    AddError(ref errors, contractType, $"{contractType.Name}.{method.Name} {requestType.Name} is missing {typeof(PolyMessageAttribute).Name}.");
                }
                else
                {
                    operation.RequestTypeID = InspectMessageType(
                        contractType, method, requestType, messageAttribute, operation, messageTypeIDs, messageTypes, ref errors);
                    operation.RequestType = requestType;
                }
            }
        }

        private static int InspectMessageType(
            Type contractType,
            MethodInfo method,
            Type messageType,
            PolyMessageAttribute messageAttribute,
            Operation operation,
            Dictionary<int, Type> messageTypeIDs,
            Dictionary<Type, Operation> messageTypes,
            ref List<PolyContractValidationError> errors)
        {
            int messageTypeID = messageAttribute.ID;
            if (messageTypeID == 0)
            {
                // we want a stable ID because lib could be used on different machines and runtimes
                // we want the ID to be >= 0
                messageTypeID = Math.Abs(GetStableHashCode(messageType.FullName));
            }

            if (messageTypeID <= 1)
            {
                AddError(ref errors, contractType, $"{contractType.Name}.{method.Name} {messageType.Name} has invalid ID of {messageTypeID}. Message IDs need to be in range [2, int.MaxValue].");
            }

            // check message is used more than once
            if (messageTypes.TryGetValue(messageType, out Operation existingOperation))
            {
                AddError(ref errors, contractType, $"{contractType.Name}.{method.Name} {messageType.Name} is already used in {existingOperation.ContractType.Name}.{existingOperation.Method.Name}.");
            }
            else
            {
                messageTypes.Add(messageType, operation);
            }

            // check message type ID uniqueness
            if (messageTypeIDs.TryGetValue(messageTypeID, out Type existingMessageType))
            {
                if (messageType != existingMessageType)
                {
                    AddError(ref errors, contractType, $"{contractType.Name}.{method.Name} {messageType.Name} has ID={messageTypeID} which is already defined for {existingMessageType.Name}.");
                }
            }
            else
            {
                messageTypeIDs.Add(messageTypeID, messageType);
            }

            return messageTypeID;
        }

        /// <summary>
        /// A hash code that is stable (does not use randomization), well distributed
        /// and returns the same value on different .NET runtimes (as long as the behavior of int ^ char does not change).
        /// https://referencesource.microsoft.com/#mscorlib/system/string.cs,827
        /// </summary>
        private static int GetStableHashCode(string @string)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < @string.Length && @string[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ @string[i];
                    if (i == @string.Length - 1 || @string[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ @string[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        private static void AddError(ref List<PolyContractValidationError> errors, Type contractType, string error)
        {
            if (errors == null)
                errors = new List<PolyContractValidationError>();
            errors.Add(new PolyContractValidationError(contractType, error));
        }
    }
}
