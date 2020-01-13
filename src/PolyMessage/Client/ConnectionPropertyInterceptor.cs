using Castle.DynamicProxy;

namespace PolyMessage.Client
{
    internal class ConnectionPropertyInterceptor : IInterceptor
    {
        private readonly PolyChannel _channel;

        public ConnectionPropertyInterceptor(PolyChannel channel)
        {
            _channel = channel;
        }

        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.ReturnType != typeof(PolyConnection) ||
                !invocation.Method.IsSpecialName ||
                !invocation.Method.Name.StartsWith("get_"))
            {
                invocation.Proceed();
                return;
            }

            invocation.ReturnValue = _channel.Connection;
        }
    }
}
