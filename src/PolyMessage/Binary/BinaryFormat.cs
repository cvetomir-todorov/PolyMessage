using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage.Binary
{
    public class BinaryFormat : PolyFormat
    {
        private readonly BinaryFormatter _formatter;
        private const string KnownErrorConnectionClosed = "End of Stream encountered before parsing was completed.";

        public BinaryFormat()
        {
            _formatter = new BinaryFormatter();
        }

        public override string DisplayName => "Binary";

        public override Task Write(object obj, PolyChannel channel, CancellationToken cancelToken)
        {
            Stream channelStream = new ChannelStream(channel);
            try
            {
                _formatter.Serialize(channelStream, obj);
                return Task.CompletedTask;
            }
            catch (SerializationException serializationException) when (serializationException.Message.StartsWith(KnownErrorConnectionClosed))
            {
                throw new PolyFormatException(PolyFormatError.EndOfDataStream, this);
            }
        }

        public override Task<object> Read(Type objType, PolyChannel channel, CancellationToken cancelToken)
        {
            Stream channelStream = new ChannelStream(channel);
            try
            {
                object obj = _formatter.Deserialize(channelStream);
                return Task.FromResult(obj);
            }
            catch (SerializationException serializationException) when (serializationException.Message.StartsWith(KnownErrorConnectionClosed))
            {
                throw new PolyFormatException(PolyFormatError.EndOfDataStream, this);
            }
        }
    }
}
