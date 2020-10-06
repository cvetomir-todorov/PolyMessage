namespace PolyMessage.Formats.MessagePack
{
    public class MessagePackFormat : PolyFormat
    {
        public override string DisplayName => "MessagePack";

        public override PolyFormatter CreateFormatter()
        {
            return new MessagePackFormatter(this);
        }
    }
}
