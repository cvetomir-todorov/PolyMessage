using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace PolyMessage.Endpoints
{
    internal interface IDispatcher
    {
        Task<object> Dispatch(object message, Endpoint endpoint);
    }

    internal sealed class DefaultDispatcher : IDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MethodInfo _castMethod;

        public DefaultDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _castMethod = GetType().GetMethod(nameof(Cast), BindingFlags.Static | BindingFlags.NonPublic);
        }

        public Task<object> Dispatch(object message, Endpoint endpoint)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                object implementor = scope.ServiceProvider.GetRequiredService(endpoint.ContractType);
                object operationTask = endpoint.Method.Invoke(implementor, new object[] {message});

                // get the response type inside of the task: when returning Task<T> we want to get T
                Type responseType = endpoint.Method.ReturnType.GenericTypeArguments[0];
                // we will cast Task<T> to Task<object> where T is the response type
                MethodInfo specificMethod = _castMethod.MakeGenericMethod(responseType);
                Task<object> resultTask = (Task<object>) specificMethod.Invoke(null, new object[] {operationTask});

                return resultTask;
            }
        }

        private static async Task<object> Cast<TSource>(Task<TSource> sourceTask)
        {
            object destination = await sourceTask.ConfigureAwait(false);
            return destination;
        }
    }
}
