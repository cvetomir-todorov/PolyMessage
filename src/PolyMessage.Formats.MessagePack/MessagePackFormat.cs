using System;
using System.IO;

namespace PolyMessage.Formats.MessagePack
{
    public class MessagePackFormat : PolyFormat
    {
        public override string DisplayName => "MessagePack";

        public override PolyFormatter CreateFormatter(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            return new MessagePackFormatter(this, stream);
        }
    }
}
