using PolyMessage.Messaging;
using PolyMessage.Metadata;

namespace PolyMessage.Server
{
    internal class ServerComponents
    {
        public ServerComponents(
            IRouter router,
            IMessageMetadata messageMetadata,
            IMessenger messenger,
            IDispatcher dispatcher)
        {
            Router = router;
            MessageMetadata = messageMetadata;
            Messenger = messenger;
            Dispatcher = dispatcher;
        }

        public IRouter Router { get; }

        public IMessageMetadata MessageMetadata { get; }

        public IMessenger Messenger { get; }

        public IDispatcher Dispatcher { get; }
    }
}
