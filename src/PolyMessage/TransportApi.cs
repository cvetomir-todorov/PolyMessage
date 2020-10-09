using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PolyMessage.Metadata;

namespace PolyMessage
{
    public abstract class PolyTransport
    {
        /// <summary>
        /// The infinite timeout as defined in <see cref="Timeout.InfiniteTimeSpan"/> field.
        /// </summary>
        public static readonly TimeSpan InfiniteTimeout = Timeout.InfiniteTimeSpan;

        public string DisplayName { get; protected set; }

        public Uri Address { get; protected set; }

        public PolyHostTimeouts HostTimeouts { get; protected set; } = new PolyHostTimeouts();

        public PolyMessageBufferSettings MessageBufferSettings { get; protected set; } = new PolyMessageBufferSettings();

        public abstract PolyListener CreateListener();

        public abstract PolyChannel CreateClient();

        public virtual IEnumerable<MessageInfo> GetMessageTypes() => new MessageInfo[0];

        protected internal IMessageMetadata MessageMetadata { get; set; }

        // TODO: change to return key-value pairs instead, modify the settings to return the pairs
        public virtual string GetSettingsInfo() => string.Empty;

        public override string ToString() => DisplayName;
    }

    public class PolyHostTimeouts
    {
        public TimeSpan ClientReceive { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan ClientSend { get; set; } = TimeSpan.FromSeconds(30);
    }

    public class PolyMessageBufferSettings
    {
        public int InitialSize { get; set; } = 8192; // 8KB
        public int MaxSize { get; set; } = int.MaxValue;
        public int MaxArraysPerBucket { get; set; } = 128;
    }

    public abstract class PolyListener : IDisposable
    {
        public void Dispose()
        {
            DoDispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DoDispose(bool isDisposing) {}

        public virtual void PrepareAccepting() {}

        public abstract Task<Func<PolyChannel>> AcceptClient();

        public abstract void StopAccepting();
    }

    public abstract class PolyChannel : IDisposable
    {
        protected PolyChannel()
        {
            MutableConnection = new PolyMutableConnection();
        }

        public void Dispose()
        {
            DoDispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DoDispose(bool isDisposing) {}

        protected internal PolyMutableConnection MutableConnection { get; }

        public PolyConnection Connection => MutableConnection;

        public abstract Task OpenAsync();

        public abstract void Close();

        public abstract Task<object> Receive(PolyFormatter formatter, string origin, CancellationToken ct);

        public abstract Task Send(object message, PolyFormatter formatter, string origin, CancellationToken ct);
    }
}
