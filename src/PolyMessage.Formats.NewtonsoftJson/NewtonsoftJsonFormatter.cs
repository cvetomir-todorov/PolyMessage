using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using PolyMessage.Exceptions;

namespace PolyMessage.Formats.NewtonsoftJson
{
    public class NewtonsoftJsonFormatter : PolyFormatter
    {
        private readonly NewtonsoftJsonFormat _format;
        private readonly Dictionary<string, StreamInstrument> _instruments;
        private readonly JsonSerializer _serializer;
        private bool _isDisposed;

        public NewtonsoftJsonFormatter(NewtonsoftJsonFormat format)
        {
            _format = format;
            _instruments = new Dictionary<string, StreamInstrument>();
            _serializer = new JsonSerializer();
        }

        protected override void DoDispose(bool isDisposing)
        {
            if (_isDisposed)
                return;

            if (isDisposing)
            {
                foreach (KeyValuePair<string, StreamInstrument> pair in _instruments)
                {
                    pair.Value.Dispose();
                }
                _isDisposed = true;
            }
        }

        public override PolyFormat Format => _format;

        public override void Serialize(object obj, string streamID, Stream stream)
        {
            StreamInstrument instrument = GetInstrument(streamID, stream);

            _serializer.Serialize(instrument.Writer, obj, obj.GetType());
            instrument.Writer.Flush();
        }

        public override object Deserialize(Type objType, string streamID, Stream stream)
        {
            StreamInstrument instrument = GetInstrument(streamID, stream);

            bool readSuccess = instrument.Reader.Read();
            if (!readSuccess)
                throw new PolyFormatException(PolyFormatError.EndOfDataStream, "Deserialization encountered end of stream.", _format);

            object obj = _serializer.Deserialize(instrument.Reader, objType);
            if (obj == null)
                throw new PolyFormatException(PolyFormatError.UnexpectedData, "Deserialization could not parse JSON token.", _format);

            return obj;
        }

        private StreamInstrument GetInstrument(string streamID, Stream stream)
        {
            if (_instruments.TryGetValue(streamID, out StreamInstrument existingInstrument))
            {
                return existingInstrument;
            }
            else
            {
                StreamInstrument newInstrument = new StreamInstrument(stream);
                _instruments.Add(streamID, newInstrument);
                return newInstrument;
            }
        }

        private sealed class StreamInstrument : IDisposable
        {
            public StreamInstrument(Stream stream)
            {
                Writer = new JsonTextWriter(new StreamWriter(stream));
                Writer.AutoCompleteOnClose = false;
                Writer.CloseOutput = false;

                Reader = new JsonTextReader(new StreamReader(stream));
                Reader.CloseInput = false;
                Reader.SupportMultipleContent = true;
            }

            public void Dispose()
            {
                Writer?.Close();
                Reader?.Close();
            }

            public JsonWriter Writer { get; }
            public JsonReader Reader { get; }
        }
    }
}
