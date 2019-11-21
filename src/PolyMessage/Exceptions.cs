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

    [Serializable]
    public class PolyConnectionClosedException : PolyException
    {
        public PolyConnectionClosedException(PolyTransport transport, Exception innerException)
            : base("Connection has been closed.", innerException)
        {
            Transport = transport;
        }

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
}
