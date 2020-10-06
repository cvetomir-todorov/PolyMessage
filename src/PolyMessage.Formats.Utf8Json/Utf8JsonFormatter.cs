using System;
using System.IO;
using PolyMessage.Exceptions;
using Utf8Json;

namespace PolyMessage.Formats.Utf8Json
{
    public class Utf8JsonFormatter : PolyFormatter
    {
        private readonly Utf8JsonFormat _format;

        public Utf8JsonFormatter(Utf8JsonFormat format)
        {
            _format = format;
        }

        public override PolyFormat Format => _format;

        public override void Serialize(object obj, string streamID, Stream stream)
        {
            JsonSerializer.NonGeneric.Serialize(obj.GetType(), stream, obj);
        }

        public override object Deserialize(Type objType, string streamID, Stream stream)
        {
            try
            {
                return JsonSerializer.NonGeneric.Deserialize(objType, stream);
            }
            catch (JsonParsingException jsonParsingException)
            {
                throw new PolyFormatException(PolyFormatError.UnexpectedData, "Deserialization encountered unexpected data.", _format, jsonParsingException);
            }
        }
    }
}
