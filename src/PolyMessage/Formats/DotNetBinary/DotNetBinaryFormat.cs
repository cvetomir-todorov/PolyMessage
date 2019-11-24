namespace PolyMessage.Formats.DotNetBinary
{
    public class DotNetBinaryFormat : PolyFormat
    {
        public override string DisplayName => "Binary";

        public override PolyFormatter CreateFormatter(PolyChannel channel)
        {
            return new DotNetBinaryFormatter(this, channel);
        }
    }
}
