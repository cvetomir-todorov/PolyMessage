using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage.Binary
{
    public class BinaryFormat : PolyFormat
    {
        private readonly BinaryFormatter _formatter;

        public BinaryFormat()
        {
            _formatter = new BinaryFormatter();
        }

        public override string DisplayName => "Binary";

        public override Task Write(object obj, PolyChannel channel, CancellationToken cancelToken)
        {
            using (Stream channelStream = new ChannelStream(channel))
            {
                _formatter.Serialize(channelStream, obj);
                return Task.CompletedTask;
            }
        }

        public override Task<object> Read(Type objType, PolyChannel channel, CancellationToken cancelToken)
        {
            using (Stream channelStream = new ChannelStream(channel))
            {
                object obj = _formatter.Deserialize(channelStream);
                return Task.FromResult(obj);
            }
        }
    }
}
