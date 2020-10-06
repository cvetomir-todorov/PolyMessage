using System;
using System.IO;
using PolyMessage.Exceptions;
using ProtoBuf;

namespace PolyMessage.Formats.ProtobufNet
{
    public class ProtobufNetFormatter : PolyFormatter
    {
        private readonly ProtobufNetFormat _format;

        public ProtobufNetFormatter(ProtobufNetFormat format)
        {
            _format = format;
        }

        public override PolyFormat Format => _format;

        public override void Serialize(object obj, string streamID, Stream stream)
        {
            Serializer.NonGeneric.Serialize(stream, obj);
        }

        public override object Deserialize(Type objType, string streamID, Stream stream)
        {
            object obj = Serializer.NonGeneric.Deserialize(objType, stream);
            if (obj == null)
                throw new PolyFormatException(PolyFormatError.EndOfDataStream, "Deserialization encountered end of stream.", _format);

            return obj;
        }
    }
}
