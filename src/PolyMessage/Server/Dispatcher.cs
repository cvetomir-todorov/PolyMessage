using System;
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
        private readonly ITaskCaster _taskCaster;

        public Dispatcher(IServiceProvider serviceProvider, ITaskCaster taskCaster)
        {
            _serviceProvider = serviceProvider;
            _taskCaster = taskCaster;
        }

        public Task<object> Dispatch(object message, Operation operation)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                object implementor = scope.ServiceProvider.GetRequiredService(operation.ContractType);
                object operationTask = operation.Method.Invoke(implementor, new object[] {message});

                // get the response type inside of the task: when returning Task<T> we want to get T
                Type responseType = operation.Method.ReturnType.GenericTypeArguments[0];
                Task<object> objectTask = _taskCaster.CastTaskResultToTaskObject(operationTask, responseType);
                return objectTask;
            }
        }
    }
}
