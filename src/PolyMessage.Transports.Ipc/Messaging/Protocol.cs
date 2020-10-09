using System;
using System.Buffers;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PolyMessage.Exceptions;
using PolyMessage.Metadata;

namespace PolyMessage.Transports.Ipc.Messaging
{
    internal class Protocol
    {
        private readonly ILogger _logger;
        private readonly IMessageMetadata _messageMetadata;
        private readonly PolyTransport _transport;
        // stream IDs
        private const string DataStreamID = "1";

        public Protocol(ILogger logger, IMessageMetadata messageMetadata, PolyTransport transport)
        {
            _logger = logger;
            _messageMetadata = messageMetadata;
            _transport = transport;
        }

        public async Task SendMmfName(string mmfName, ArrayPool<byte> bufferPool, PipeStream pipeStream, string origin, CancellationToken ct)
        {
            _logger.LogTrace("[{0}] Sending MMF name {1}...", origin, mmfName);
            byte[] buffer = null;

            try
            {
                int byteCount = Encoding.UTF8.GetByteCount(mmfName);
                buffer = bufferPool.Rent(4 + byteCount);

                EncodeInt32(byteCount, buffer, offset: 0);
                Encoding.UTF8.GetBytes(mmfName, charIndex: 0, charCount: mmfName.Length, buffer, byteIndex: 4);

                await pipeStream.WriteAsync(buffer, offset: 0, buffer.Length, ct).ConfigureAwait(false);
                await pipeStream.FlushAsync(ct).ConfigureAwait(false);
                _logger.LogTrace("[{0}] Sent MMF name {1} with {2} bytes length.", origin, mmfName, byteCount);
            }
            finally
            {
                if (buffer != null)
                {
                    bufferPool.Return(buffer);
                }
            }
        }

        public async Task<string> ReceiveMmfName(ArrayPool<byte> bufferPool, PipeStream pipeStream, string origin, CancellationToken ct)
        {
            byte[] buffer = null;

            try
            {
                buffer = bufferPool.Rent(minimumLength: 4);
                await ReadBytes(pipeStream, buffer, offset: 0, count: 4, ct).ConfigureAwait(false);

                int mmfNameLength = DecodeInt32(buffer, offset: 0);
                _logger.LogTrace("[{0}] Received MMF name length {1}.", origin, mmfNameLength);

                bufferPool.Return(buffer);
                buffer = bufferPool.Rent(mmfNameLength);
                await ReadBytes(pipeStream, buffer, offset: 0, count: mmfNameLength, ct).ConfigureAwait(false);

                string mmfName = Encoding.UTF8.GetString(buffer, index: 0, count: mmfNameLength);
                _logger.LogTrace("[{0}] Received MMF name {1}.", origin, mmfName);

                return mmfName;
            }
            finally
            {
                if (buffer != null)
                {
                    bufferPool.Return(buffer);
                }
            }
        }

        public async Task SendMessage(
            object message, PolyFormatter formatter,
            ArrayPool<byte> bufferPool, PipeStream controlStream, InMemoryStream dataStream, MemoryMappedViewStream mmfStream,
            string origin, CancellationToken ct)
        {
            byte[] buffer = null;
            dataStream.Origin = origin;

            try
            {
                buffer = bufferPool.Rent(minimumLength: 8);
                short messageTypeID = _messageMetadata.GetMessageTypeID(message.GetType());
                ProtocolHeader header = new ProtocolHeader{MessageTypeID = messageTypeID};

                await SendObject(
                        ProtocolCommands.Header, header, ProtocolHeader.TypeID, "header", formatter,
                        buffer, controlStream, dataStream, mmfStream, origin, ct)
                    .ConfigureAwait(false);
                await SendObject(
                        ProtocolCommands.Message, message, messageTypeID, "message", formatter,
                        buffer, controlStream, dataStream, mmfStream, origin, ct)
                    .ConfigureAwait(false);

            }
            finally
            {
                if (buffer != null)
                {
                    bufferPool.Return(buffer);
                }
            }
        }

        private async Task SendObject(
            byte command, object obj, int objTypeID, string objName, PolyFormatter formatter,
            byte[] buffer, PipeStream controlStream, InMemoryStream dataStream, MemoryMappedViewStream mmfStream,
            string origin, CancellationToken ct)
        {
            dataStream.ResetLengthAndPosition();
            formatter.Serialize(obj, DataStreamID, dataStream);

            if (mmfStream.Length - mmfStream.Position < dataStream.Length)
            {
                _logger.LogTrace("[{0}] Sending rewind command...", origin);
                buffer[0] = ProtocolCommands.Rewind;
                await controlStream.WriteAsync(buffer, offset: 0, count: 1, ct).ConfigureAwait(false);
                await controlStream.FlushAsync(ct).ConfigureAwait(false);
                _logger.LogTrace("[{0}] Sent rewind command.", origin);
                mmfStream.Position = 0;
            }

            _logger.LogTrace("[{0}] Sending command {1} for {2} with type ID {3}...", origin, command, objName, objTypeID);

            buffer[0] = command;
            //await controlStream.WriteAsync(buffer, offset: 0, count: 1, ct).ConfigureAwait(false);
            //await controlStream.FlushAsync(ct).ConfigureAwait(false);
            EncodeInt32((int) dataStream.Length, buffer, offset: 1);
            await controlStream.WriteAsync(buffer, offset: 0, count: 5, ct).ConfigureAwait(false);
            await controlStream.FlushAsync(ct).ConfigureAwait(false);
            await dataStream.SendToTransport(ct).ConfigureAwait(false);

            _logger.LogTrace("[{0}] Sent command {1} for {2} with type ID {3}.", origin, command, objName, objTypeID);
        }

        public async Task<object> ReceiveMessage(
            PolyFormatter formatter,
            ArrayPool<byte> bufferPool, Stream controlStream, InMemoryStream dataStream, MemoryMappedViewStream mmfStream,
            string origin, CancellationToken ct)
        {
            byte[] buffer = null;
            dataStream.Origin = origin;

            try
            {
                buffer = bufferPool.Rent(minimumLength: 8);

                ProtocolHeader header = (ProtocolHeader) await ReceiveObject(
                        ProtocolCommands.Header, "header", typeof(ProtocolHeader), formatter,
                        buffer, controlStream, dataStream, mmfStream, origin, ct)
                    .ConfigureAwait(false);
                Type messageType = _messageMetadata.GetMessageType(header.MessageTypeID);
                object message = await ReceiveObject(
                        ProtocolCommands.Message, "message", messageType, formatter,
                        buffer, controlStream, dataStream, mmfStream, origin, ct)
                    .ConfigureAwait(false);
                return message;
            }
            finally
            {
                if (buffer != null)
                {
                    bufferPool.Return(buffer);
                }
            }
        }

        private async Task<object> ReceiveObject(
            byte expectedCommand, string objName, Type objType,
            PolyFormatter formatter, byte[] buffer,
            Stream controlStream, InMemoryStream dataStream, MemoryMappedViewStream mmfStream,
            string origin, CancellationToken ct)
        {
            byte command = await ReceiveCommand(buffer, controlStream, origin, ct).ConfigureAwait(false);
            if (command == ProtocolCommands.Rewind)
            {
                _logger.LogTrace("[{0}] Received rewind command.", origin);
                mmfStream.Position = 0;
                command = await ReceiveCommand(buffer, controlStream, origin, ct).ConfigureAwait(false);
            }
            if (command != expectedCommand)
            {
                // TODO: throw protocol exception
                throw new InvalidOperationException($"Expected command {expectedCommand} but received command {command}.");
            }

            await ReadBytes(controlStream, buffer, offset: 0, count: 4, ct).ConfigureAwait(false);
            int objSize = DecodeInt32(buffer, offset: 0);
            _logger.LogTrace("[{0}] Received {1} bytes for {2}.", origin, objSize, objName);

            await dataStream.ReceiveFromTransport(objSize, objName, ct).ConfigureAwait(false);
            dataStream.PrepareForDeserialize(objSize);
            object obj = formatter.Deserialize(objType, DataStreamID, dataStream);
            _logger.LogTrace("[{0}] Deserialized {1} bytes as {2}.", origin, objSize, objName);

            return obj;
        }

        private async Task<byte> ReceiveCommand(byte[] buffer, Stream controlStream, string origin, CancellationToken ct)
        {
            _logger.LogTrace("[{0}] Receiving command...", origin);
            await ReadBytes(controlStream, buffer, offset: 0, count: 1, ct).ConfigureAwait(false);
            byte command = buffer[0];
            _logger.LogTrace("[{0}] Received command {1}.", origin, command);

            return command;
        }

        private async Task ReadBytes(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int totalBytesRead = 0;
            int bytesRemaining = count;

            while (totalBytesRead < count)
            {
                int bytesRead = await stream.ReadAsync(buffer, offset, bytesRemaining, ct).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new PolyConnectionClosedException(PolyConnectionCloseReason.ConnectionClosed, _transport);
                }

                totalBytesRead += bytesRead;
                offset += bytesRead;
                bytesRemaining -= bytesRead;
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

        private static class ProtocolCommands
        {
            /// <summary>
            /// Rewind reading from the MMF.
            /// </summary>
            public static readonly byte Rewind = 1;
            public static readonly byte Header = 2;
            public static readonly byte Message = 3;
        }
    }
}
