using PolyMessage.Metadata;
using PolyMessage.Timer;

namespace PolyMessage.Server
{
    internal class ServerComponents
    {
        public ServerComponents(
            IRouter router,
            IMessageMetadata messageMetadata,
            ITimer timer,
            IDispatcher dispatcher)
        {
            Router = router;
            MessageMetadata = messageMetadata;
            Timer = timer;
            Dispatcher = dispatcher;
        }

        public IRouter Router { get; }

        public IMessageMetadata MessageMetadata { get; }

        public ITimer Timer { get; }

        public IDispatcher Dispatcher { get; }
    }
}
