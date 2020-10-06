namespace PolyMessage.Formats.Utf8Json
{
    public class Utf8JsonFormat : PolyFormat
    {
        public override string DisplayName => "Utf8Json";

        public override PolyFormatter CreateFormatter()
        {
            return new Utf8JsonFormatter(this);
        }
    }
}
