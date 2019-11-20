using System;
using System.IO;
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

        public abstract string DisplayName { get; }

        public abstract Task PrepareAccepting();

        public abstract Task<PolyChannel> AcceptClient();

        public abstract void StopAccepting();
    }

    public abstract class PolyChannel : IDisposable
    {
        public void Dispose() => DoDispose(true);

        protected virtual void DoDispose(bool isDisposing) {}

        public abstract string DisplayName { get; }

        public abstract void Open();

        public abstract Uri LocalAddress { get; }

        public abstract Uri RemoteAddress { get; }

        // FEAT: hide this and expose just send/receive byte[]
        public abstract Stream Stream { get; }
    }

    public enum CommunicationState
    {
        Created, Opened, Closed
    }
}
