using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PolyMessage.Messaging
{
    internal sealed class MessagingStream : Stream
    {
        private readonly ILogger _logger;
        private readonly PolyChannel _channel;
        private readonly ArrayPool<byte> _pool;
        private readonly int _capacity;
        private readonly byte[] _lengthPrefixBuffer;
        private readonly byte[] _dataBuffer;
        private int _position;
        private int _length;
        private const int LengthPrefixSize = 4;

        public MessagingStream(PolyChannel channel, ArrayPool<byte> bufferPool, int capacity, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _channel = channel;
            _pool = bufferPool;
            _capacity = capacity;
            _lengthPrefixBuffer = _pool.Rent(LengthPrefixSize);
            _dataBuffer = _pool.Rent(_capacity);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pool.Return(_lengthPrefixBuffer);
                _pool.Return(_dataBuffer);
            }
        }
        public override bool CanRead => true;

        public override int ReadByte()
        {
            if (_position >= _length)
            {
                return 0;
            }

            byte data = _dataBuffer[_position];
            _position++;
            return data;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remainingBytes = _length - _position;
            if (remainingBytes <= 0)
                return 0;

            int bytesToCopy = remainingBytes < count ? remainingBytes : count;
            Array.Copy(sourceArray: _dataBuffer, sourceIndex: _position, destinationArray: buffer, destinationIndex: offset, bytesToCopy);
            _position += bytesToCopy;
            return bytesToCopy;
        }

        public override bool CanWrite => true;

        public override void WriteByte(byte value)
        {
            if (_position >= _dataBuffer.Length)
                ThrowMessageTooBig();

            _dataBuffer[_position] = value;
            _position++;
            _length++;// position cannot be changed so it is correct
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            long bytesRemaining = _dataBuffer.Length - _position;
            if (bytesRemaining < count)
                ThrowMessageTooBig();

            Array.Copy(sourceArray: buffer, sourceIndex: offset, destinationArray: _dataBuffer, destinationIndex: _position, count);
            _position += count;
            _length += count;// position cannot be changed so it is correct
        }

        public override void Flush() {}

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Reset()
        {
            _position = 0;
            _length = 0;
        }

        public async Task WriteMessageToTransport(string origin, CancellationToken cancellationToken)
        {
            EncodeInt32(_length, _lengthPrefixBuffer, offset: 0);

            await _channel.WriteAsync(_lengthPrefixBuffer, 0, LengthPrefixSize, cancellationToken).ConfigureAwait(false);
            await _channel.WriteAsync(_dataBuffer, 0, _length, cancellationToken).ConfigureAwait(false);
            await _channel.FlushAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogTrace("[{0}] Sent value {1} for length prefix and {2} bytes for message.", origin, _position, _length);
        }

        public async Task ReadMessageFromTransport(string origin, CancellationToken cancellationToken)
        {
            int bytesRead = await _channel.ReadAsync(_lengthPrefixBuffer, 0, LengthPrefixSize, cancellationToken).ConfigureAwait(false);
            if (bytesRead != LengthPrefixSize)
                ThrowUnexpectedBytesRead(bytesRead, LengthPrefixSize, "length prefix");

            int lengthPrefix = DecodeInt32(_lengthPrefixBuffer, offset: 0);
            _logger.LogTrace("[{0}] Received {1} bytes length prefix.", origin, lengthPrefix);
            if (lengthPrefix > _capacity)
                ThrowMessageTooBig();

            if (lengthPrefix == 0)
            {
                bytesRead = 0;
            }
            else
            {
                bytesRead = await _channel.ReadAsync(_dataBuffer, 0, lengthPrefix, cancellationToken).ConfigureAwait(false);
                if (bytesRead != lengthPrefix)
                    ThrowUnexpectedBytesRead(bytesRead, lengthPrefix, "message");
            }

            _logger.LogTrace("[{0}] Received {1} bytes for message.", origin, bytesRead);
            _length = lengthPrefix;
            _position = 0;
        }

        private void ThrowMessageTooBig()
        {
            throw new InvalidOperationException($"Message is too big. Maximum size is {_capacity} bytes.");
        }

        private void ThrowUnexpectedBytesRead(int bytesRead, int bytesExpected, string representation)
        {
            throw new InvalidOperationException($"Received {bytesRead} bytes for {representation} instead of the expected {bytesExpected} bytes.");
        }

        private static void EncodeInt32(int value, byte[] destination, int offset)
        {
            // encode using big endian
            destination[offset] = (byte)(value >> 24);
            destination[offset + 1] = (byte)(value >> 16);
            destination[offset + 2] = (byte)(value >> 8);
            destination[offset + 3] = (byte)value;
        }

        private static int DecodeInt32(byte[] source, int offset)
        {
            // encode using big endian
            int i0 = source[offset] << 24;
            int i1 = source[offset + 1] << 16;
            int i2 = source[offset + 2] << 8;
            int i3 = source[offset + 3];

            int lengthPrefix = i0 + i1 + i2 + i3;
            return lengthPrefix;
        }

        public override long Length => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override bool CanSeek => false;

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    }
}
