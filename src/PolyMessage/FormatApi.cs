using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage
{
    public abstract class PolyFormat
    {
        public abstract string DisplayName { get; }

        public virtual void RegisterMessageTypes(IEnumerable<MessageInfo> messageTypes) { }

        public abstract PolyFormatter CreateFormatter(PolyChannel channel);

        public override string ToString()
        {
            return DisplayName;
        }
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

        public abstract Task Write(object obj, CancellationToken cancelToken);

        public abstract Task<object> Read(Type objType, CancellationToken cancelToken);

        public override string ToString()
        {
            return $"Formatter for {Format.DisplayName}";
        }
    }
}
