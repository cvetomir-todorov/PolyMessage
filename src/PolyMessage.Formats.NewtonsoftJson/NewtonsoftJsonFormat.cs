namespace PolyMessage.Formats.NewtonsoftJson
{
    public class NewtonsoftJsonFormat : PolyFormat
    {
        public override string DisplayName => "NewtonsoftJSON";

        public override PolyFormatter CreateFormatter(PolyChannel channel)
        {
            return new NewtonsoftJsonFormatter(this, channel);
        }
    }
}
