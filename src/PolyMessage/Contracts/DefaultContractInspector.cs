using System;
using System.Collections.Generic;
using System.Reflection;
using PolyMessage.Endpoints;

namespace PolyMessage.Contracts
{
    internal interface IContractInspector
    {
        IEnumerable<Endpoint> InspectContract(Type contractType, Type implementationType);
    }

    internal sealed class DefaultContractInspector : IContractInspector
    {
        public IEnumerable<Endpoint> InspectContract(Type contractType, Type implementationType)
        {
            // TODO: check valid attributes and types etc.

            MethodInfo[] methods = contractType.GetMethods(BindingFlags.Instance | BindingFlags.Public);

            foreach (MethodInfo method in methods)
            {
                Endpoint endpoint = new Endpoint();

                endpoint.Path = $"{contractType.FullName}.{method.Name}";
                endpoint.ImplementationType = implementationType;
                endpoint.Method = method;

                yield return endpoint;
            }
        }
    }
}
