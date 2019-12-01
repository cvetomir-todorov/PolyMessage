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
        private readonly PolyStream _channelStream;
        private readonly JsonWriter _writer;
        private readonly JsonReader _reader;
        private readonly JsonSerializer _serializer;
        private bool _isDisposed;

        public NewtonsoftJsonFormatter(NewtonsoftJsonFormat format, PolyChannel channel)
        {
            _format = format;
            _channelStream = new PolyStream(channel);

            _writer = new JsonTextWriter(new StreamWriter(_channelStream));
            _writer.AutoCompleteOnClose = false;
            _writer.CloseOutput = false;

            _reader = new JsonTextReader(new StreamReader(_channelStream));
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
                _channelStream.Dispose();
                _isDisposed = true;
            }
        }

        public override PolyFormat Format => _format;

        public override Task Write(object obj, CancellationToken cancelToken)
        {
            _serializer.Serialize(_writer, obj, obj.GetType());
            return _writer.FlushAsync(cancelToken);
        }

        public override async Task<object> Read(Type objType, CancellationToken cancelToken)
        {
            await _reader.ReadAsync(cancelToken);
            object obj = _serializer.Deserialize(_reader, objType);
            if (obj == null)
                throw new PolyFormatException(PolyFormatError.EndOfDataStream, "Deserialization encountered end of stream.", _format);
            else
                return obj;
        }
    }
}
