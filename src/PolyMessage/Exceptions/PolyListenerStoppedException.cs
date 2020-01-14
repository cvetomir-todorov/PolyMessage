using System;

namespace PolyMessage.Exceptions
{
    [Serializable]
    public class PolyListenerStoppedException : PolyException
    {
        public PolyListenerStoppedException(PolyTransport transport, Exception exception)
            : base($"The listener for transport {transport.DisplayName} stopped.", exception)
        {
            Transport = transport;
        }

        public PolyTransport Transport { get; }
    }
}
