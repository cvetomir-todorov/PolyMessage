using System;
using System.Reflection;

namespace PolyMessage.Endpoints
{
    internal sealed class Endpoint
    {
        public int RequestID { get; set; }

        public Type RequestType { get; set; }

        public int ResponseID { get; set; }

        public Type ResponseType { get; set; }

        public MethodInfo Method { get; set; }

        public Type ContractType { get; set; }
    }
}
