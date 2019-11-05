using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PolyMessage.Metadata;

namespace PolyMessage.Server
{
    internal interface IDispatcher
    {
        Task<object> Dispatch(object message, Operation operation);
    }

    internal sealed class Dispatcher : IDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MethodInfo _castMethod;

        public Dispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _castMethod = GetType().GetMethod(nameof(Cast), BindingFlags.Static | BindingFlags.NonPublic);
        }

        public Task<object> Dispatch(object message, Operation operation)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                object implementor = scope.ServiceProvider.GetRequiredService(operation.ContractType);
                object operationTask = operation.Method.Invoke(implementor, new object[] {message});

                // get the response type inside of the task: when returning Task<T> we want to get T
                Type responseType = operation.Method.ReturnType.GenericTypeArguments[0];
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
