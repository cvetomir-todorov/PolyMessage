using System;
using System.Reflection;

namespace PolyMessage.Metadata
{
    internal sealed class Operation
    {
        public int RequestID { get; set; }

        public Type RequestType { get; set; }

        public int ResponseID { get; set; }

        public Type ResponseType { get; set; }

        public MethodInfo Method { get; set; }

        public Type ContractType { get; set; }

        public override string ToString()
        {
            return $"{ContractType} Operation={Method.Name} Request={RequestType.Name}(ID={RequestID}) Response={ResponseType.Name}(ID={ResponseID})";
        }
    }
}
