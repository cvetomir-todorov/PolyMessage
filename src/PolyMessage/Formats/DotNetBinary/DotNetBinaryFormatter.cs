using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using PolyMessage.Exceptions;

namespace PolyMessage.Formats.DotNetBinary
{
    public sealed class DotNetBinaryFormatter : PolyFormatter
    {
        private readonly DotNetBinaryFormat _format;
        private readonly BinaryFormatter _formatter;
        private const string KnownErrorEndOfStream = "End of Stream encountered before parsing was completed.";

        public DotNetBinaryFormatter(DotNetBinaryFormat format)
        {
            _format = format;
            _formatter = new BinaryFormatter();
        }

        public override PolyFormat Format => _format;

        public override void Serialize(object obj, string streamID, Stream stream)
        {
            _formatter.Serialize(stream, obj);
        }

        public override object Deserialize(Type objType, string streamID, Stream stream)
        {
            try
            {
                return _formatter.Deserialize(stream);
            }
            catch (SerializationException serializationException) when (serializationException.Message.StartsWith(KnownErrorEndOfStream))
            {
                throw new PolyFormatException(PolyFormatError.EndOfDataStream, "Deserialization encountered end of stream.", _format);
            }
        }
    }
}
