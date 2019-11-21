using System;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage
{
    public abstract class PolyFormat
    {
        public abstract string DisplayName { get; }

        public abstract Task Write(object obj, PolyChannel channel, CancellationToken cancelToken);

        public abstract Task<object> Read(Type objType, PolyChannel channel, CancellationToken cancelToken);
    }

    public abstract class PolyTransport
    {
        public abstract string DisplayName { get; }

        public abstract Uri Address { get; }

        public abstract PolyListener CreateListener();

        public abstract PolyChannel CreateClient();
    }

    public abstract class PolyListener : IDisposable
    {
        public void Dispose() => DoDispose(true);

        protected virtual void DoDispose(bool isDisposing){}

        public abstract Task PrepareAccepting();

        public abstract Task<PolyChannel> AcceptClient();

        public abstract void StopAccepting();
    }

    public abstract class PolyChannel : IDisposable
    {
        public void Dispose() => DoDispose(true);

        protected virtual void DoDispose(bool isDisposing) {}

        public abstract PolyConnection Connection { get; }

        public abstract void Open();

        public abstract void Close();

        public abstract int Read(byte[] buffer, int offset, int count);

        public abstract Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancelToken);

        public abstract void Write(byte[] buffer, int offset, int count);

        public abstract Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancelToken);

        public abstract void Flush();

        public abstract Task FlushAsync(CancellationToken cancelToken);
    }
}
