using System;
using System.Collections.Generic;
using System.Text;

namespace PolyMessage.Exceptions
{
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

}
