namespace PolyMessage.Formats.MessagePack
{
    public class MessagePackFormat : PolyFormat
    {
        public override string DisplayName => "MessagePack";

        public override PolyFormatter CreateFormatter(PolyChannel channel)
        {
            return new MessagePackFormatter(this, channel);
        }
    }
}
