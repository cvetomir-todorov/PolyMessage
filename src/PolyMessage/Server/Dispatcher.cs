using System;
using System.Threading.Tasks;
using PolyMessage.CodeGeneration;
using PolyMessage.Metadata;

namespace PolyMessage.Server
{
    internal interface IDispatcher
    {
        Task<object> Dispatch(object implementor, object message, Operation operation);
    }

    internal sealed class Dispatcher : IDispatcher
    {
        private readonly IMessageMetadata _messageMetadata;
        private readonly DispatchRequest _dispatchRequest;

        public Dispatcher(
            IMessageMetadata messageMetadata,
            DispatchRequest dispatchRequest)
        {
            _messageMetadata = messageMetadata;
            _dispatchRequest = dispatchRequest;
        }

        public Task<object> Dispatch(object implementor, object message, Operation operation)
        {
            Type responseType = operation.Method.ReturnType.GenericTypeArguments[0];
            int responseTypeID = _messageMetadata.GetMessageTypeID(responseType);

            // code generation is used to avoid using reflection at runtime
            Task<object> operationTask = _dispatchRequest(responseTypeID, message, implementor);
            return operationTask;
        }
    }
}
