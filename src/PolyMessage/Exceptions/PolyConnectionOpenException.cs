using System;

namespace PolyMessage.Exceptions
{
    [Serializable]
    public class PolyConnectionOpenException : PolyException
    {
        public PolyConnectionOpenException(PolyTransport transport, Exception inner)
            : base($"Transport {transport.DisplayName} connection failed to open. See inner exception for details.", inner)
        {
            Transport = transport;
        }

        public PolyTransport Transport { get; }
    }
}
