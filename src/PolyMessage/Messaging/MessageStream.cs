using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PolyMessage.Messaging
{
    internal sealed class MessageStream : Stream
    {
        private readonly string _origin;
        private readonly ILogger _logger;
        private readonly PolyChannel _channel;
        private readonly ArrayPool<byte> _pool;
        private byte[] _messageBuffer;
        private int _position;
        private int _length;
        private const int LengthPrefixSize = 4;

        public MessageStream(string origin, PolyChannel channel, ArrayPool<byte> bufferPool, int capacity, ILoggerFactory loggerFactory)
        {
            _origin = origin;
            _logger = loggerFactory.CreateLogger(GetType());
            _channel = channel;
            _pool = bufferPool;
            _messageBuffer = _pool.Rent(capacity);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pool.Return(_messageBuffer);
            }
        }

        public override bool CanRead => true;

        public override int ReadByte()
        {
            if (_position >= _length)
            {
                return 0;
            }

            byte data = _messageBuffer[_position];
            _position++;
            return data;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRemaining = _length - _position;
            if (bytesRemaining <= 0)
            {
                return 0;
            }

            int bytesToCopy = bytesRemaining < count ? bytesRemaining : count;
            Array.Copy(sourceArray: _messageBuffer, sourceIndex: _position, destinationArray: buffer, destinationIndex: offset, bytesToCopy);
            _position += bytesToCopy;
            return bytesToCopy;
        }

        public override bool CanWrite => true;

        public override void WriteByte(byte value)
        {
            if (_position >= _messageBuffer.Length)
            {
                ExpandBuffer(copyExistingContent: true, targetCapacity: _position + 1);
            }

            _messageBuffer[_position] = value;
            _position++;
            _length++;// position cannot be changed so it is correct
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            long bytesFree = _messageBuffer.Length - _position;
            if (bytesFree < count)
            {
                ExpandBuffer(copyExistingContent: true, targetCapacity: _position + count);
            }

            Array.Copy(sourceArray: buffer, sourceIndex: offset, destinationArray: _messageBuffer, destinationIndex: _position, count);
            _position += count;
            _length += count;// position cannot be changed so it is correct
        }

        public override void Flush() {}

        public override Task FlushAsync(CancellationToken ct) => Task.CompletedTask;

        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public void ResetLengthAndPosition()
        {
            _position = 0;
            _length = 0;
        }

        public void ReserveSpaceForLengthPrefix()
        {
            if (_position + LengthPrefixSize >= _messageBuffer.Length)
            {
                ExpandBuffer(copyExistingContent: true, targetCapacity: _position + LengthPrefixSize);
            }
            _position += LengthPrefixSize;
            _length += LengthPrefixSize;
        }

        public void WriteLengthPrefix(int position, string target)
        {
            int lengthPrefix = _length - position - LengthPrefixSize;
            EncodeInt32(lengthPrefix, _messageBuffer, position);
            _logger.LogTrace("[{0}] Encoded {1} for {2} length prefix value.", _origin, lengthPrefix, target);
        }

        public async Task SendToTransport(CancellationToken ct)
        {
            await _channel.WriteAsync(_messageBuffer, 0, _length, ct).ConfigureAwait(false);
            await _channel.FlushAsync(ct).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Sent {1} bytes from the buffer.", _origin, _length);
        }

        public async Task<int> ReceiveFromTransport(string target, CancellationToken ct)
        {
            int bytesRead = await ReadBytes(_messageBuffer, 0, LengthPrefixSize, ct).ConfigureAwait(false);
            if (bytesRead != LengthPrefixSize)
            {
                throw CreateUnexpectedBytesReadException(bytesRead, LengthPrefixSize, $"{target} length prefix");
            }

            int lengthPrefix = DecodeInt32(_messageBuffer, offset: 0);
            _logger.LogTrace("[{0}] Received {1} for {2} length prefix value.", _origin, lengthPrefix, target);
            if (lengthPrefix + LengthPrefixSize > _messageBuffer.Length)
            {
                ExpandBuffer(copyExistingContent: false, targetCapacity: lengthPrefix + LengthPrefixSize);
            }

            if (lengthPrefix == 0)
            {
                bytesRead = 0;
            }
            else
            {
                bytesRead = await ReadBytes(_messageBuffer, LengthPrefixSize, lengthPrefix, ct).ConfigureAwait(false);
                if (bytesRead != lengthPrefix)
                {
                    throw CreateUnexpectedBytesReadException(bytesRead, lengthPrefix, target);
                }
            }

            _logger.LogTrace("[{0}] Received {1} bytes for {2}.", _origin, bytesRead, target);
            return lengthPrefix;
        }

        public void PrepareForDeserialize(int messageLength)
        {
            _length = messageLength + LengthPrefixSize;
            _position = LengthPrefixSize;
        }

        private async Task<int> ReadBytes(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int totalBytesRead = 0;
            int bytesRemaining = count;

            while (totalBytesRead < count)
            {
                int bytesRead = await _channel.ReadAsync(buffer, offset, bytesRemaining, ct).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    // TODO: fix exception handling
                    throw new PolyFormatException(PolyFormatError.EndOfDataStream, "connection closed or timed-out", null);
                }

                totalBytesRead += bytesRead;
                offset += bytesRead;
                bytesRemaining -= bytesRead;
            }

            return totalBytesRead;
        }

        private Exception CreateUnexpectedBytesReadException(int bytesRead, int bytesExpected, string representation)
        {
            return new InvalidOperationException($"Received {bytesRead} bytes for {representation} instead of the expected {bytesExpected} bytes.");
        }

        private void ExpandBuffer(bool copyExistingContent, int targetCapacity)
        {
            int newCapacity = _messageBuffer.Length;
            while (newCapacity < targetCapacity)
            {
                newCapacity *= 2;
            }

            byte[] previousDataBuffer = _messageBuffer;
            bool isNewDataBufferRented = false;

            try
            {
                _messageBuffer = _pool.Rent(newCapacity);
                isNewDataBufferRented = true;
                _logger.LogTrace("[{0}] Expanded buffer to {1} bytes.", _origin, newCapacity);

                if (copyExistingContent)
                {
                    Array.Copy(previousDataBuffer, 0, _messageBuffer, 0, _length);
                    _logger.LogTrace("[{0}] Copied {1} bytes into expanded buffer.", _origin, _length);
                }
            }
            finally
            {
                if (isNewDataBufferRented)
                {
                    _pool.Return(previousDataBuffer);
                }
            }
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

        public override void SetLength(long value) => throw new NotSupportedException();

        public override bool CanSeek => false;

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    }
}
