using System;
using System.Threading;

namespace PolyMessage.Tcp
{
    public sealed class TcpSettings
    {
        public static readonly TimeSpan InfiniteTimeout = Timeout.InfiniteTimeSpan;
        private TimeSpan _receiveTimeout;
        private TimeSpan _sendTimeout;

        public TcpSettings()
        {
            _receiveTimeout = InfiniteTimeout;
            _sendTimeout = InfiniteTimeout;
            NoDelay = true;
        }

        public TimeSpan ReceiveTimeout
        {
            get { return _receiveTimeout; }
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentException("Timeout should be greater than zero.");
                _receiveTimeout = value;
            }
        }

        public TimeSpan SendTimeout
        {
            get { return _sendTimeout; }
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentException("Timeout should be greater than zero.");
                _sendTimeout = value;
            }
        }

        public bool NoDelay { get; set; }
    }
}
