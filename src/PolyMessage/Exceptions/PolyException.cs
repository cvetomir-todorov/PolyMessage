using System;

namespace PolyMessage.Exceptions
{
    [Serializable]
    public abstract class PolyException : Exception
    {
        protected PolyException(string message) : base(message)
        {}

        protected PolyException(string message, Exception inner) : base(message, inner)
        {}
    }
}