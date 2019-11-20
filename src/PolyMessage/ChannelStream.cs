using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage
{
    internal sealed class ChannelStream : Stream
    {
        private readonly PolyChannel _channel;

        public ChannelStream(PolyChannel channel)
        {
            _channel = channel;
        }

        public override bool CanRead => true;

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _channel.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _channel.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override bool CanWrite => true;

        public override void Write(byte[] buffer, int offset, int count)
        {
            _channel.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _channel.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void Flush()
        {
            _channel.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _channel.FlushAsync(cancellationToken);
        }

        public override bool CanSeek => false;

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}
