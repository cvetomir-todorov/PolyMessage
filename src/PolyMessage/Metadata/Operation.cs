using System;
using System.Reflection;

namespace PolyMessage.Metadata
{
    internal sealed class Operation
    {
        public short RequestTypeID { get; set; }

        public Type RequestType { get; set; }

        public short ResponseTypeID { get; set; }

        public Type ResponseType { get; set; }

        public MethodInfo Method { get; set; }

        public Type ContractType { get; set; }

        public override string ToString()
        {
            return $"{ContractType.Name} Operation={Method.Name} Request={RequestType.Name}({RequestTypeID}) Response={ResponseType.Name}({ResponseTypeID})";
        }
    }
}
