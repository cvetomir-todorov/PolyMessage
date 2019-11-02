using System;
using System.Reflection;

namespace PolyMessage.Endpoints
{
    internal sealed class Endpoint
    {
        public string Path { get; set; }
        public Type ImplementationType { get; set; }
        public MethodInfo Method { get; set; }
    }
}
