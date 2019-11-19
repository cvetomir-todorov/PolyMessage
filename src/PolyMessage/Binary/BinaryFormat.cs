using System;
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
            _formatter.Serialize(channel.Stream, obj);
            return Task.CompletedTask;
        }

        public override Task<object> Read(Type objType, PolyChannel channel, CancellationToken cancelToken)
        {
            object obj = _formatter.Deserialize(channel.Stream);
            return Task.FromResult(obj);
        }
    }
}
