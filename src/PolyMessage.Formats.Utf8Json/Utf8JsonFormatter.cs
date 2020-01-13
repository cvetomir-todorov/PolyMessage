using System;
using System.IO;
using Utf8Json;

namespace PolyMessage.Formats.Utf8Json
{
    public class Utf8JsonFormatter : PolyFormatter
    {
        private readonly Utf8JsonFormat _format;
        private readonly Stream _stream;

        public Utf8JsonFormatter(Utf8JsonFormat format, Stream stream)
        {
            _format = format;
            _stream = stream;
        }

        public override PolyFormat Format => _format;

        public override void Serialize(object obj)
        {
            JsonSerializer.NonGeneric.Serialize(obj.GetType(), _stream, obj);
            _stream.Flush();
        }

        public override object Deserialize(Type objType)
        {
            try
            {
                return JsonSerializer.NonGeneric.Deserialize(objType, _stream);
            }
            catch (JsonParsingException jsonParsingException)
            {
                throw new PolyFormatException(PolyFormatError.UnexpectedData, "Deserialization encountered unexpected data.", _format, jsonParsingException);
            }
        }
    }
}
