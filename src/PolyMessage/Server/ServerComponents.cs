using PolyMessage.Messaging;
using PolyMessage.Metadata;
using PolyMessage.Timer;

namespace PolyMessage.Server
{
    internal class ServerComponents
    {
        public ServerComponents(
            IRouter router,
            IMessageMetadata messageMetadata,
            IMessenger messenger,
            ITimer timer,
            IDispatcher dispatcher)
        {
            Router = router;
            MessageMetadata = messageMetadata;
            Messenger = messenger;
            Timer = timer;
            Dispatcher = dispatcher;
        }

        public IRouter Router { get; }

        public IMessageMetadata MessageMetadata { get; }

        public IMessenger Messenger { get; }

        public ITimer Timer { get; }

        public IDispatcher Dispatcher { get; }
    }
}
