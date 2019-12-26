using System;
using System.Collections.Generic;
using System.IO;

namespace PolyMessage
{
    public abstract class PolyFormat
    {
        public abstract string DisplayName { get; }

        public virtual void RegisterMessageTypes(IEnumerable<MessageInfo> messageTypes) {}

        public abstract PolyFormatter CreateFormatter(Stream stream);

        public override string ToString() => DisplayName;
    }

    public struct MessageInfo
    {
        public MessageInfo(Type type, short typeID)
        {
            Type = type;
            TypeID = typeID;
        }

        public Type Type { get; }
        public short TypeID { get; }

        public override string ToString() => $"{Type.Name}({TypeID})";
    }

    public abstract class PolyFormatter : IDisposable
    {
        public void Dispose()
        {
            DoDispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DoDispose(bool isDisposing) { }

        public abstract PolyFormat Format { get; }

        public abstract void Serialize(object obj);

        public abstract object Deserialize(Type objType);

        public override string ToString() => $"Formatter[{Format.DisplayName}]";
    }
}
