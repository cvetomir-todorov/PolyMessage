using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;

namespace PolyMessage.Formats.MessagePack
{
    public class MessagePackFormat : PolyFormat
    {
        public override string DisplayName => "MessagePack";

        public override Task Write(object obj, PolyChannel channel, CancellationToken cancelToken)
        {
            using (Stream channelStream = new PolyStream(channel))
            {
                MessagePackSerializer.NonGeneric.Serialize(obj.GetType(), channelStream, obj, MessagePackSerializer.DefaultResolver);
                channelStream.Flush();
                return Task.CompletedTask;
            }
        }

        public override Task<object> Read(Type objType, PolyChannel channel, CancellationToken cancelToken)
        {
            using (Stream channelStream = new PolyStream(channel))
            {
                // FEAT: using readStrict is slow, but their API is limited
                object obj = MessagePackSerializer.NonGeneric.Deserialize(objType, channelStream, MessagePackSerializer.DefaultResolver, readStrict: true);
                return Task.FromResult(obj);
            }
        }
    }
}
