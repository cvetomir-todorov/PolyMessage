using System;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;

namespace PolyMessage.Formats.MessagePack
{
    public class MessagePackFormatter : PolyFormatter
    {
        private readonly MessagePackFormat _format;
        private readonly PolyStream _channelStream;

        public MessagePackFormatter(MessagePackFormat format, PolyChannel channel)
        {
            _format = format;
            _channelStream = new PolyStream(channel);
        }

        protected override void DoDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _channelStream.Dispose();
            }
        }

        public override PolyFormat Format => _format;

        public override Task Write(object obj, CancellationToken cancelToken)
        {
            MessagePackSerializer.NonGeneric.Serialize(obj.GetType(), _channelStream, obj, MessagePackSerializer.DefaultResolver);
            _channelStream.Flush();
            return Task.CompletedTask;
        }

        public override Task<object> Read(Type objType, CancellationToken cancelToken)
        {
            // FEAT: using readStrict is slow, but their API is limited
            object obj = MessagePackSerializer.NonGeneric.Deserialize(objType, _channelStream, MessagePackSerializer.DefaultResolver, readStrict: true);
            return Task.FromResult(obj);
        }
    }
}
