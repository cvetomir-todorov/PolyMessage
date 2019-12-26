using System;
using System.IO;
using ProtoBuf;

namespace PolyMessage.Formats.ProtobufNet
{
    public class ProtobufNetFormatter : PolyFormatter
    {
        private readonly ProtobufNetFormat _format;
        private readonly Stream _stream;

        public ProtobufNetFormatter(ProtobufNetFormat format, Stream stream)
        {
            _format = format;
            _stream = stream;
        }

        public override PolyFormat Format => _format;

        public override void Serialize(object obj)
        {
            Serializer.NonGeneric.Serialize(_stream, obj);
            _stream.Flush();
        }

        public override object Deserialize(Type objType)
        {
            object obj = Serializer.NonGeneric.Deserialize(objType, _stream);
            if (obj == null)
                throw new PolyFormatException(PolyFormatError.EndOfDataStream, "Deserialization encountered end of stream.", _format);

            return obj;
        }
    }
}
