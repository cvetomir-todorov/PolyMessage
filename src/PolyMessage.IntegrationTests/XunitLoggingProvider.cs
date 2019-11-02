﻿using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace PolyMessage.IntegrationTests
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
            _output.WriteLine("{0} | {1} | {2}", logLevel, _category, formatter(state, exception));
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