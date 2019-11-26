using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PolyMessage.Formats.NewtonsoftJson
{
    public class NewtonsoftJsonFormatter : PolyFormatter
    {
        private readonly NewtonsoftJsonFormat _format;
        private readonly PolyStream _stream;
        private readonly JsonWriter _writer;
        private readonly JsonReader _reader;
        private readonly JsonSerializer _serializer;
        private bool _isDisposed;

        public NewtonsoftJsonFormatter(NewtonsoftJsonFormat format, PolyChannel channel)
        {
            _format = format;
            _stream = new PolyStream(channel);

            _writer = new JsonTextWriter(new StreamWriter(_stream));
            _writer.AutoCompleteOnClose = false;
            _writer.CloseOutput = false;

            _reader = new JsonTextReader(new StreamReader(_stream));
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
                _stream.Dispose();
                _isDisposed = true;
            }
        }

        public override PolyFormat Format => _format;

        public override Task Write(object obj, CancellationToken cancelToken)
        {
            _serializer.Serialize(_writer, obj, obj.GetType());
            _writer.Flush();
            return Task.CompletedTask;
        }

        public override Task<object> Read(Type objType, CancellationToken cancelToken)
        {
            _reader.Read();
            object obj = _serializer.Deserialize(_reader, objType);
            if (obj == null)
                throw new PolyFormatException(PolyFormatError.EndOfDataStream, _format);
            else
                return Task.FromResult(obj);
        }
    }
}
