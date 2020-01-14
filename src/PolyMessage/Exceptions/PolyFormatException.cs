using System;

namespace PolyMessage.Exceptions
{
    public enum PolyFormatError
    {
        EndOfDataStream,
        UnexpectedData
    }

    [Serializable]
    public class PolyFormatException : PolyException
    {
        public PolyFormatException(PolyFormatError formatError, string errorDetail, PolyFormat format)
            : base(CreateMessage(formatError, errorDetail, format))
        {
            FormatError = formatError;
            ErrorDetail = errorDetail;
            Format = format;
        }

        public PolyFormatException(PolyFormatError formatError, string errorDetail, PolyFormat format, Exception inner)
            : base(CreateMessage(formatError, errorDetail, format), inner)
        {
            FormatError = formatError;
            ErrorDetail = errorDetail;
            Format = format;
        }

        private static string CreateMessage(PolyFormatError formatError, string errorDetail, PolyFormat format)
        {
            return $"Format {format.DisplayName} encountered an error {formatError}. {errorDetail}";
        }

        public PolyFormatError FormatError { get; }
        public string ErrorDetail { get; }
        public PolyFormat Format { get; }
    }
}