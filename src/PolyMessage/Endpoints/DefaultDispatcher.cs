using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace PolyMessage.Endpoints
{
    internal interface IDispatcher
    {
        Task<string> Dispatch(string message, Endpoint endpoint);
    }

    internal sealed class DefaultDispatcher : IDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<string> Dispatch(string message, Endpoint endpoint)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                object implementor = scope.ServiceProvider.GetRequiredService(endpoint.ImplementationType);
                Task<string> resultTask = (Task<string>) endpoint.Method.Invoke(implementor, new object[] {message});
                return resultTask;
            }
        }
    }
}
