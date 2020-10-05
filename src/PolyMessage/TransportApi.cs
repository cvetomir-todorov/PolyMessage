using System;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage
{
    public abstract class PolyTransport
    {
        /// <summary>
        /// The infinite timeout as defined in <see cref="Timeout.InfiniteTimeSpan"/> field.
        /// </summary>
        public static readonly TimeSpan InfiniteTimeout = Timeout.InfiniteTimeSpan;

        public abstract string DisplayName { get; }

        public abstract Uri Address { get; }

        public PolyHostTimeouts HostTimeouts { get; protected set; } = new PolyHostTimeouts();

        public PolyMessageBufferSettings MessageBufferSettings { get; protected set; } = new PolyMessageBufferSettings();

        public abstract PolyListener CreateListener();

        public abstract PolyChannel CreateClient();

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
        public int MaxSize { get; set; } = int.MaxValue; // used only on the server
    }

    public abstract class PolyListener : IDisposable
    {
        public void Dispose()
        {
            DoDispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DoDispose(bool isDisposing) {}

        public abstract void PrepareAccepting();

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

        public abstract PolyTransport Transport { get; }

        public abstract Task OpenAsync();

        public abstract void Close();

        public abstract Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct);

        public abstract Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct);

        public abstract Task FlushAsync(CancellationToken ct);
    }
}
