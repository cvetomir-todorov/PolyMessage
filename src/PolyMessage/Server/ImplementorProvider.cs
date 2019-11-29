using System;
using Microsoft.Extensions.DependencyInjection;

namespace PolyMessage.Server
{
    internal interface IImplementorProvider : IDisposable
    {
        void OperationStarted();

        object ResolveImplementor(Type contractType);

        void OperationFinished();
    }

    internal sealed class ImplementorProvider : IImplementorProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private IServiceScope _currentScope;

        public ImplementorProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Dispose()
        {
            _currentScope?.Dispose();
        }

        public void OperationStarted()
        {
            if (_currentScope != null)
                throw new InvalidOperationException($"{nameof(OperationFinished)} should be called first.");
            _currentScope = _serviceProvider.CreateScope();
        }

        public object ResolveImplementor(Type contractType)
        {
            if (_currentScope == null)
                throw new InvalidOperationException("Missing scope.");

            object implementor = _currentScope.ServiceProvider.GetRequiredService(contractType);
            return implementor;
        }

        public void OperationFinished()
        {
            if (_currentScope == null)
                throw new InvalidOperationException($"{nameof(OperationStarted)} should be called first.");
            _currentScope.Dispose();
            _currentScope = null;
        }
    }
}
