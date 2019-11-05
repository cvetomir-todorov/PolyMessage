using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage
{
    public interface IFormat
    {
        string DisplayName { get; }

        Task Write(object obj, IChannel channel, CancellationToken cancelToken);

        Task<object> Read(Type objType, IChannel channel, CancellationToken cancelToken);
    }

    public interface ITransport
    {
        string DisplayName { get; }

        Uri Address { get; }

        TimeSpan ReceiveTimeout { get; set; }

        TimeSpan SendTimeout { get; set; }

        IListener CreateListener();

        IChannel CreateClient();
    }

    public interface IListener : IDisposable
    {
        string DisplayName { get; }

        Task PrepareAccepting();

        Task<IChannel> AcceptClient();

        void StopAccepting();
    }

    public interface IChannel : IDisposable
    {
        string DisplayName { get; }

        // FEAT: hide this and expose just send/receive byte[]
        Stream Stream { get; }
    }

    public enum CommunicationState
    {
        Created, Opened, Closed
    }
}
