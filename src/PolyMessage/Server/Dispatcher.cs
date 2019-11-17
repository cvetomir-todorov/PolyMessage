using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PolyMessage.CodeGeneration;
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
        private readonly IMessageMetadata _messageMetadata;
        private readonly CastTaskOfResponseToTaskOfObject _castDelegate;

        public Dispatcher(
            IServiceProvider serviceProvider,
            IMessageMetadata messageMetadata,
            ICodeGenerator codeGenerator)
        {
            _serviceProvider = serviceProvider;
            _messageMetadata = messageMetadata;
            _castDelegate = codeGenerator.GetCastTaskOfResponseToTaskOfObjectDelegate();
        }

        public Task<object> Dispatch(object message, Operation operation)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                object implementor = scope.ServiceProvider.GetRequiredService(operation.ContractType);
                object operationTask = operation.Method.Invoke(implementor, new object[] {message});

                Type responseType = operation.Method.ReturnType.GenericTypeArguments[0];
                int responseID = _messageMetadata.GetMessageID(responseType);
                Task<object> objectTask = _castDelegate(responseID, (Task) operationTask);
                return objectTask;
            }
        }
    }
}
