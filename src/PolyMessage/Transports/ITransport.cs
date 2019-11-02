using System;
using System.Threading.Tasks;
using PolyMessage.Formats;

namespace PolyMessage.Transports
{
    // TODO: apply interface segregation for server/client
    public interface ITransport : IDisposable
    {
        string DisplayName { get; }

        Task PrepareAccepting();

        Task<IChannel> AcceptClient(IFormat format);

        void StopAccepting();

        IChannel CreateClient(IFormat format);
    }
}
