using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage.Formats.DotNetBinary
{
    public sealed class DotNetBinaryFormatter : PolyFormatter
    {
        private readonly DotNetBinaryFormat _format;
        private readonly BinaryFormatter _formatter;
        private readonly PolyStream _channelStream;
        private const string KnownErrorConnectionClosed = "End of Stream encountered before parsing was completed.";

        public DotNetBinaryFormatter(DotNetBinaryFormat format, PolyChannel channel)
        {
            _format = format;
            _formatter = new BinaryFormatter();
            _channelStream = new PolyStream(channel);
        }

        protected override void DoDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _channelStream.Dispose();
            }
        }

        public override PolyFormat Format => _format;

        public override Task Write(object obj, CancellationToken cancelToken)
        {
            try
            {
                _formatter.Serialize(_channelStream, obj);
                return Task.CompletedTask;
            }
            catch (SerializationException serializationException) when (serializationException.Message.StartsWith(KnownErrorConnectionClosed))
            {
                throw new PolyFormatException(PolyFormatError.EndOfDataStream, _format);
            }
        }

        public override Task<object> Read(Type objType, CancellationToken cancelToken)
        {
            try
            {
                object obj = _formatter.Deserialize(_channelStream);
                return Task.FromResult(obj);
            }
            catch (SerializationException serializationException) when (serializationException.Message.StartsWith(KnownErrorConnectionClosed))
            {
                throw new PolyFormatException(PolyFormatError.EndOfDataStream, _format);
            }
        }
    }
}
