using System.IO;

namespace PolyMessage.Formats.MessagePack
{
    public class MessagePackFormat : PolyFormat
    {
        public override string DisplayName => "MessagePack";

        public override PolyFormatter CreateFormatter(Stream stream)
        {
            return new MessagePackFormatter(this, stream);
        }
    }
}
