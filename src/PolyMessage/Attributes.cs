using System;

namespace PolyMessage
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class PolyContractAttribute : Attribute
    {}

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PolyRequestResponseAttribute : Attribute
    {}

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PolyMessageAttribute : Attribute
    {
        public int ID { get; set; }
    }
}
