using System;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;

namespace PolyMessage.Formats.ProtobufNet
{
    public class ProtobufNetFormatter : PolyFormatter
    {
        private readonly ProtobufNetFormat _format;
        private readonly PolyStream _polyStream;

        public ProtobufNetFormatter(ProtobufNetFormat format, PolyChannel channel)
        {
            _format = format;
            _polyStream = new PolyStream(channel);
        }

        protected override void DoDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _polyStream.Dispose();
            }
        }

        public override PolyFormat Format => _format;

        public override Task Write(object obj, CancellationToken cancelToken)
        {
            int fieldNumber = _format.GetFieldNumber(obj.GetType());
            Serializer.NonGeneric.SerializeWithLengthPrefix(_polyStream, obj, PrefixStyle.Base128, fieldNumber);
            return Task.CompletedTask;
        }

        public override Task<object> Read(Type objType, CancellationToken cancelToken)
        {
            Serializer.NonGeneric.TryDeserializeWithLengthPrefix(_polyStream, PrefixStyle.Base128, _format.TypeResolver, out object obj);
            return Task.FromResult(obj);
        }
    }
}
