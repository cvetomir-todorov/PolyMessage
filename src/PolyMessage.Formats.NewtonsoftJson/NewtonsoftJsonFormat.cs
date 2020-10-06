namespace PolyMessage.Formats.NewtonsoftJson
{
    public class NewtonsoftJsonFormat : PolyFormat
    {
        public override string DisplayName => "NewtonsoftJSON";

        public override PolyFormatter CreateFormatter()
        {
            return new NewtonsoftJsonFormatter(this);
        }
    }
}
