using System;
using System.Threading.Tasks;
using PolyMessage.Formats;

namespace PolyMessage.Transports
{
    public interface IListener : IDisposable
    {
        Task PrepareAccepting();

        Task<IChannel> AcceptClient(IFormat format);

        void StopAccepting();
    }
}
