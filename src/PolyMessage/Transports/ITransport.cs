using PolyMessage.Formats;

namespace PolyMessage.Transports
{
    public interface ITransport
    {
        string DisplayName { get; }

        IListener CreateListener();

        IChannel CreateClient(IFormat format);
    }
}
