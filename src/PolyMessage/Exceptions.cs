using System;
using System.IO;

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

    [Serializable]
    public class PolyConnectionException : PolyException
    {
        public static PolyConnectionException ConnectionClosed(IOException ioException)
        {
            return new PolyConnectionException("Connection has been closed.", ioException);
        }

        public PolyConnectionException(string message) : base(message)
        {}

        public PolyConnectionException(string message, Exception innerException) : base(message, innerException)
        {}
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
}
