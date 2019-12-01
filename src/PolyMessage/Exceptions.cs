using System;
using System.Collections.Generic;
using System.Text;

namespace PolyMessage
{
    [Serializable]
    public abstract class PolyException : Exception
    {
        protected PolyException(string message) : base(message)
        {}

        protected PolyException(string message, Exception inner) : base(message, inner)
        {}
    }

    public sealed class PolyContractValidationError
    {
        internal PolyContractValidationError(Type contractType, string error)
        {
            ContractType = contractType;
            Error = error;
        }

        public Type ContractType { get; }
        public string Error { get; }
    }

    [Serializable]
    public class PolyContractException : PolyException
    {
        public PolyContractException(IReadOnlyCollection<PolyContractValidationError> validationErrors)
            : base($"Validating contract(s) resulted in {validationErrors.Count} validation errors.")
        {
            ValidationErrors = validationErrors;
        }

        public IReadOnlyCollection<PolyContractValidationError> ValidationErrors { get; }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            foreach (PolyContractValidationError validationError in ValidationErrors)
            {
                builder.AppendLine(validationError.Error);
            }

            return builder.ToString();
        }
    }

    public enum PolyConnectionCloseReason
    {
        RemoteTimedOut, RemoteAbortedConnection, ConnectionReset
    }

    [Serializable]
    public class PolyConnectionClosedException : PolyException
    {
        public PolyConnectionClosedException(PolyConnectionCloseReason closeReason, PolyTransport transport, Exception innerException)
            : base($"Transport {transport.DisplayName} connection has been closed with reason {closeReason}.", innerException)
        {
            CloseReason = closeReason;
            Transport = transport;
        }

        public PolyConnectionCloseReason CloseReason { get; }
        public PolyTransport Transport { get; }
    }

    [Serializable]
    public class PolyOpenConnectionException : PolyException
    {
        public PolyOpenConnectionException(PolyTransport transport, Exception inner)
            : base($"Transport {transport.DisplayName} connection failed to open. See inner exception for details.", inner)
        {
            Transport = transport;
        }

        public PolyTransport Transport { get; }
    }

    [Serializable]
    public class PolyListenerStoppedException : PolyException
    {
        public PolyListenerStoppedException(PolyTransport transport, Exception exception) : base("The listener stopped.", exception)
        {
            Transport = transport;
        }

        public PolyTransport Transport { get; }
    }

    public enum PolyFormatError
    {
        EndOfDataStream, TypeRegistration
    }

    [Serializable]
    public class PolyFormatException : PolyException
    {
        public PolyFormatException(PolyFormatError formatError, string errorDetail, PolyFormat format)
            : base($"Format {format.DisplayName} reported an error {formatError}. {errorDetail}")
        {
            FormatError = formatError;
            ErrorDetail = errorDetail;
            Format = format;
        }

        public PolyFormatError FormatError { get; }
        public string ErrorDetail { get; }
        public PolyFormat Format { get; }
    }
}
