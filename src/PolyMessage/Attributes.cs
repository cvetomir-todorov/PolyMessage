using System;

namespace PolyMessage
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class PolyContract : Attribute
    {}

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PolyRequestResponseEndpoint : Attribute
    {}
}
