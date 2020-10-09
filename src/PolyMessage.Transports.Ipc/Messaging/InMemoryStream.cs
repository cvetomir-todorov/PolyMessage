using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PolyMessage.Transports.Ipc.Messaging
{
    internal class InMemoryStream : Stream
    {
        private readonly ILogger _logger;
        private readonly Stream _internalStream;
        private readonly ArrayPool<byte> _pool;
        private string _origin;
        private byte[] _messageBuffer;
        private int _position;
        private int _length;

        // TODO: move internal stream as a parameter for send to/ receive from transport methods
        public InMemoryStream(ILogger logger, Stream internalStream, ArrayPool<byte> bufferPool, int capacity)
        {
            _logger = logger;
            _internalStream = internalStream;
            _pool = bufferPool;
            _messageBuffer = _pool.Rent(capacity);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pool.Return(_messageBuffer);
            }

            base.Dispose(disposing);
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

        public string Origin
        {
            get => _origin;
            set => _origin = value;
        }

        public void ResetLengthAndPosition()
        {
            _position = 0;
            _length = 0;
        }

        public async Task SendToTransport(CancellationToken ct)
        {
            await _internalStream.WriteAsync(_messageBuffer, 0, _length, ct).ConfigureAwait(false);
            await _internalStream.FlushAsync(ct).ConfigureAwait(false);
            _logger.LogTrace("[{0}] Sent {1} bytes from the buffer.", _origin, _length);
        }

        public void PrepareForDeserialize(int messageLength)
        {
            _length = messageLength;
            _position = 0;
        }

        public async Task<int> ReceiveFromTransport(int messageLength, string target, CancellationToken ct)
        {
            if (messageLength > _messageBuffer.Length)
            {
                ExpandBuffer(copyExistingContent: false, targetCapacity: messageLength);
            }

            int bytesRead;
            if (messageLength == 0)
            {
                bytesRead = 0;
            }
            else
            {
                bytesRead = await ReadBytes(_messageBuffer, offset: 0, count: messageLength, ct).ConfigureAwait(false);
                if (bytesRead < 0)
                {
                    return bytesRead;
                }
                if (bytesRead != messageLength)
                {
                    throw CreateUnexpectedBytesReadException(bytesRead, messageLength, target);
                }
            }

            _logger.LogTrace("[{0}] Received {1} bytes for {2}.", _origin, bytesRead, target);
            return messageLength;
        }

        private async Task<int> ReadBytes(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int totalBytesRead = 0;
            int bytesRemaining = count;

            while (totalBytesRead < count)
            {
                int bytesRead = await _internalStream.ReadAsync(buffer, offset, bytesRemaining, ct).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    return -1;
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

        public override void SetLength(long value) => throw new NotSupportedException();

        public override bool CanSeek => false;

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    }
}
