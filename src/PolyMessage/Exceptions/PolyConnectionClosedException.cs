using System;

namespace PolyMessage.Exceptions
{
    public enum PolyConnectionCloseReason
    {
        /// <summary>
        /// Occurs when there is an unexpected transport error.
        /// </summary>
        Unexpected,
        /// <summary>
        /// Occurs when an established connection is aborted by the remote.
        /// </summary>
        ConnectionAborted,
        /// <summary>
        /// Occurs when the remote crashes.
        /// </summary>
        ConnectionReset,
        /// <summary>
        /// Occurs when the remote closes the connection and no more data is available.
        /// </summary>
        ConnectionClosed
    }

    [Serializable]
    public class PolyConnectionClosedException : PolyException
    {
        public PolyConnectionClosedException(PolyConnectionCloseReason closeReason, PolyTransport transport)
            : base(CreateMessage(closeReason, transport))
        {
            CloseReason = closeReason;
            Transport = transport;
        }

        public PolyConnectionClosedException(PolyConnectionCloseReason closeReason, PolyTransport transport, Exception innerException)
            : base(CreateMessage(closeReason, transport), innerException)
        {
            CloseReason = closeReason;
            Transport = transport;
        }

        private static string CreateMessage(PolyConnectionCloseReason closeReason, PolyTransport transport)
        {
            return $"Transport {transport.DisplayName} connection has been closed with reason {closeReason}.";
        }

        public PolyConnectionCloseReason CloseReason { get; }
        public PolyTransport Transport { get; }
    }
}
