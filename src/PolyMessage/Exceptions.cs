using System;

namespace PolyMessage
{
    [Serializable]
    public abstract class PolyException : Exception
    {
        protected PolyException(string message) : base(message)
        {}

        protected PolyException(string message, Exception inner) : base(message, inner)
        {}
    }

    public enum PolyConnectionCloseReason
    {
        RemoteTimedOut, RemoteAbortedConnection
    }

    [Serializable]
    public class PolyConnectionClosedException : PolyException
    {
        public PolyConnectionClosedException(PolyConnectionCloseReason closeReason, PolyTransport transport, Exception innerException)
            : base($"Transport {transport.DisplayName} connection has been closed with reason {closeReason}.", innerException)
        {
            CloseReason = closeReason;
            Transport = transport;
        }

        public PolyConnectionCloseReason CloseReason { get; }
        public PolyTransport Transport { get; }
    }

    [Serializable]
    public class PolyListenerStoppedException : PolyException
    {
        public PolyListenerStoppedException(PolyTransport transport, Exception exception) : base("The listener stopped.", exception)
        {
            Transport = transport;
        }

        public PolyTransport Transport { get; }
    }

    public enum PolyFormatError
    {
        EndOfDataStream
    }

    [Serializable]
    public class PolyFormatException : PolyException
    {
        public PolyFormatException(PolyFormatError formatError, PolyFormat format)
            : base($"Format {format.DisplayName} IO operation resulted in an error {formatError}.")
        {
            FormatError = formatError;
            Format = format;
        }

        public PolyFormatError FormatError { get; }
        public PolyFormat Format { get; }
    }
}
