using System;
using System.IO;
using MessagePack;

namespace PolyMessage.Formats.MessagePack
{
    public class MessagePackFormatter : PolyFormatter
    {
        private readonly MessagePackFormat _format;
        private readonly Stream _stream;
        private const string KnownErrorInvalidCode = "Invalid MessagePack code was detected";

        public MessagePackFormatter(MessagePackFormat format, Stream stream)
        {
            _format = format;
            _stream = stream;
        }

        public override PolyFormat Format => _format;

        public override void Serialize(object obj)
        {
            MessagePackSerializer.NonGeneric.Serialize(obj.GetType(), _stream, obj);
            _stream.Flush();
        }

        public override object Deserialize(Type objType)
        {
            try
            {
                return MessagePackSerializer.NonGeneric.Deserialize(objType, _stream);
            }
            catch (InvalidOperationException exception) when (exception.Message.StartsWith(KnownErrorInvalidCode))
            {
                throw new PolyFormatException(PolyFormatError.UnexpectedData, "Deserialization encountered invalid code.", _format, exception);
            }
        }
    }
}
