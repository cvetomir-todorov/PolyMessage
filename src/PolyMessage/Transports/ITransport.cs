using System;
using System.Threading.Tasks;
using PolyMessage.Formats;

namespace PolyMessage.Transports
{
    public interface ITransport : IDisposable
    {
        string DisplayName { get; }

        Task PrepareAccepting();

        Task<IChannel> AcceptClient(IFormat format);

        void StopAccepting();

        IChannel CreateClient(IFormat format);
    }
}
