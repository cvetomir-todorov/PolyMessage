using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace PolyMessage.Tests
{
    public sealed class XunitLoggingProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _output;

        public XunitLoggingProvider(ITestOutputHelper output)
        {
            _output = output;
        }

        public void Dispose()
        {}

        public ILogger CreateLogger(string category)
        {
            return new XunitLogger(category, _output);
        }
    }
}