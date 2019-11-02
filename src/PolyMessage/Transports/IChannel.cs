using System;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage.Transports
{
    public interface IChannel : IDisposable
    {
        Task Send(string message, CancellationToken cancelToken);

        Task<string> Receive(CancellationToken cancelToken);
    }
}
