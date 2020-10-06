using System;
using System.IO;
using MessagePack;
using PolyMessage.Exceptions;

namespace PolyMessage.Formats.MessagePack
{
    public class MessagePackFormatter : PolyFormatter
    {
        private readonly MessagePackFormat _format;
        private const string KnownErrorInvalidCode = "Invalid MessagePack code was detected";

        public MessagePackFormatter(MessagePackFormat format)
        {
            _format = format;
        }

        public override PolyFormat Format => _format;

        public override void Serialize(object obj, string streamID, Stream stream)
        {
            MessagePackSerializer.NonGeneric.Serialize(obj.GetType(), stream, obj);
        }

        public override object Deserialize(Type objType, string streamID, Stream stream)
        {
            try
            {
                return MessagePackSerializer.NonGeneric.Deserialize(objType, stream);
            }
            catch (InvalidOperationException exception) when (exception.Message.StartsWith(KnownErrorInvalidCode))
            {
                throw new PolyFormatException(PolyFormatError.UnexpectedData, "Deserialization encountered invalid code.", _format, exception);
            }
        }
    }
}
