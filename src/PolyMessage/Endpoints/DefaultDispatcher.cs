using System;
using System.Threading.Tasks;

namespace PolyMessage.Endpoints
{
    internal interface IDispatcher
    {
        Task<string> Dispatch(string message, Endpoint endpoint);
    }

    internal sealed class DefaultDispatcher : IDispatcher
    {
        public Task<string> Dispatch(string message, Endpoint endpoint)
        {
            // TODO: get this from a DI or check constructors
            object implementor = Activator.CreateInstance(endpoint.ImplementationType);
            Task<string> resultTask = (Task<string>) endpoint.Method.Invoke(implementor, new object[] {message});
            return resultTask;
        }
    }
}
