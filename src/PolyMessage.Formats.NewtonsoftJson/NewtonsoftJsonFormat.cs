namespace PolyMessage.Formats.NewtonsoftJson
{
    public class NewtonsoftJsonFormat : PolyFormat
    {
        public override string DisplayName => "Newtonsoft JSON";

        public override PolyFormatter CreateFormatter(PolyChannel channel)
        {
            return new NewtonsoftJsonFormatter(this, channel);
        }
    }
}
