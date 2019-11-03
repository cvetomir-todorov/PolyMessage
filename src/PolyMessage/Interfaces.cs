using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage
{
    public interface IFormat
    {
        string DisplayName { get; }

        Task WriteToStream(string message, Stream stream, CancellationToken cancelToken);

        Task<string> ReadFromStream(Stream stream, CancellationToken cancelToken);
    }

    public interface ITransport
    {
        string DisplayName { get; }

        TimeSpan ReceiveTimeout { get; set; }

        TimeSpan SendTimeout { get; set; }

        IListener CreateListener();

        IChannel CreateClient(IFormat format);
    }

    public interface IListener : IDisposable
    {
        Task PrepareAccepting();

        Task<IChannel> AcceptClient(IFormat format);

        void StopAccepting();
    }

    public interface IChannel : IDisposable
    {
        Task Send(string message, CancellationToken cancelToken);

        Task<string> Receive(CancellationToken cancelToken);
    }
}
