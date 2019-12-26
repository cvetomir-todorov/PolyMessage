using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace PolyMessage.Formats.DotNetBinary
{
    public sealed class DotNetBinaryFormatter : PolyFormatter
    {
        private readonly DotNetBinaryFormat _format;
        private readonly BinaryFormatter _formatter;
        private readonly Stream _stream;
        private const string KnownErrorConnectionClosed = "End of Stream encountered before parsing was completed.";

        public DotNetBinaryFormatter(DotNetBinaryFormat format, Stream stream)
        {
            _format = format;
            _formatter = new BinaryFormatter();
            _stream = stream;
        }

        public override PolyFormat Format => _format;

        public override void Serialize(object obj)
        {
            _formatter.Serialize(_stream, obj);
            _stream.Flush();
        }

        public override object Deserialize(Type objType)
        {
            try
            {
                return _formatter.Deserialize(_stream);
            }
            catch (SerializationException serializationException) when (serializationException.Message.StartsWith(KnownErrorConnectionClosed))
            {
                throw new PolyFormatException(PolyFormatError.EndOfDataStream, "Deserialization encountered end of stream.", _format);
            }
        }
    }
}
