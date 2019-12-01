using System;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;

namespace PolyMessage.Formats.ProtobufNet
{
    public class ProtobufNetFormatter : PolyFormatter
    {
        private readonly ProtobufNetFormat _format;
        private readonly PolyStream _channelStream;
        private bool _isDisposed;

        public ProtobufNetFormatter(ProtobufNetFormat format, PolyChannel channel)
        {
            _format = format;
            _channelStream = new PolyStream(channel);
        }

        protected override void DoDispose(bool isDisposing)
        {
            if (_isDisposed)
                return;

            if (isDisposing)
            {
                _channelStream.Dispose();
                _isDisposed = true;
            }
        }

        public override PolyFormat Format => _format;

        public override Task Write(object obj, CancellationToken cancelToken)
        {
            int fieldNumber = _format.GetFieldNumber(obj.GetType());
            Serializer.NonGeneric.SerializeWithLengthPrefix(_channelStream, obj, PrefixStyle.Base128, fieldNumber);
            return Task.CompletedTask;
        }

        public override Task<object> Read(Type objType, CancellationToken cancelToken)
        {
            Serializer.NonGeneric.TryDeserializeWithLengthPrefix(_channelStream, PrefixStyle.Base128, _format.TypeResolver, out object obj);
            if (obj == null)
                throw new PolyFormatException(PolyFormatError.EndOfDataStream, "Deserialization encountered end of stream.", _format);
            else
                return Task.FromResult(obj);
        }
    }
}
