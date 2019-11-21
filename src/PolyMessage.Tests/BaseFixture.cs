using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace PolyMessage.Tests
{
    public abstract class BaseFixture : IDisposable
    {
        protected IServiceProvider ServiceProvider { get; }
        protected ILogger Logger { get; }

        protected BaseFixture(ITestOutputHelper output) : this(output, collection => {})
        {}

        protected BaseFixture(ITestOutputHelper output, Action<IServiceCollection> addServices)
        {
            ServiceProvider = CreateServiceProvider(output, addServices);
            Logger = ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
        }

        public void Dispose()
        {
            Dispose(disposingInsteadOfFinalizing: true);
        }

        protected virtual void Dispose(bool disposingInsteadOfFinalizing)
        {}

        private static IServiceProvider CreateServiceProvider(ITestOutputHelper output, Action<IServiceCollection> addServices)
        {
            IServiceCollection services = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.SetMinimumLevel(LogLevel.Information);
                    loggingBuilder.AddDebug();
                    loggingBuilder.AddProvider(new XunitLoggingProvider(output));
                });
            addServices(services);
            return services.BuildServiceProvider();
        }
    }
}
