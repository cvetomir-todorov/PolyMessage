using System;
using System.IO;
using Newtonsoft.Json;

namespace PolyMessage.Formats.NewtonsoftJson
{
    public class NewtonsoftJsonFormatter : PolyFormatter
    {
        private readonly NewtonsoftJsonFormat _format;
        private readonly JsonWriter _writer;
        private readonly JsonReader _reader;
        private readonly JsonSerializer _serializer;
        private bool _isDisposed;

        public NewtonsoftJsonFormatter(NewtonsoftJsonFormat format, Stream stream)
        {
            _format = format;

            _writer = new JsonTextWriter(new StreamWriter(stream));
            _writer.AutoCompleteOnClose = false;
            _writer.CloseOutput = false;

            _reader = new JsonTextReader(new StreamReader(stream));
            _reader.CloseInput = false;
            _reader.SupportMultipleContent = true;

            _serializer = new JsonSerializer();
        }

        protected override void DoDispose(bool isDisposing)
        {
            if (_isDisposed)
                return;

            if (isDisposing)
            {
                _writer.Close();
                _reader.Close();
                _isDisposed = true;
            }
        }

        public override PolyFormat Format => _format;

        public override void Serialize(object obj)
        {
            _serializer.Serialize(_writer, obj, obj.GetType());
            _writer.Flush();
        }

        public override object Deserialize(Type objType)
        {
            bool readSuccess = _reader.Read();
            if (!readSuccess)
                throw new PolyFormatException(PolyFormatError.EndOfDataStream, "Deserialization encountered end of stream.", _format);

            object obj = _serializer.Deserialize(_reader, objType);
            if (obj == null)
                throw new PolyFormatException(PolyFormatError.UnexpectedData, "Deserialization could not parse JSON token.", _format);

            return obj;
        }
    }
}
