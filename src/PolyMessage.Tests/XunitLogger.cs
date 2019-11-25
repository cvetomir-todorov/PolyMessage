using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace PolyMessage.Tests
{
    public sealed class XunitLogger : ILogger, IDisposable
    {
        private readonly string _category;
        private readonly ITestOutputHelper _output;

        public XunitLogger(string category, ITestOutputHelper output)
        {
            _category = category;
            _output = output;
        }

        public void Dispose()
        {}

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (exception != null)
            {
                _output.WriteLine("{0} | {1} | {2} {3}", logLevel, _category, formatter(state, exception), exception);
            }
            else
            {
                _output.WriteLine("{0} | {1} | {2}", logLevel, _category, formatter(state, exception));
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }
    }
}