using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage.Binary
{
    public class BinaryFormat : IFormat
    {
        private readonly BinaryFormatter _formatter;

        public BinaryFormat()
        {
            _formatter = new BinaryFormatter();
        }

        public string DisplayName => "Binary";

        public Task Write(object obj, IChannel channel, CancellationToken cancelToken)
        {
            _formatter.Serialize(channel.Stream, obj);
            return Task.CompletedTask;
        }

        public Task<object> Read(Type objType, IChannel channel, CancellationToken cancelToken)
        {
            object obj = _formatter.Deserialize(channel.Stream);
            return Task.FromResult(obj);
        }
    }
}
