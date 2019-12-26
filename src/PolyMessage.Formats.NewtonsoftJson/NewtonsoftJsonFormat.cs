using System.IO;

namespace PolyMessage.Formats.NewtonsoftJson
{
    public class NewtonsoftJsonFormat : PolyFormat
    {
        public override string DisplayName => "NewtonsoftJSON";

        public override PolyFormatter CreateFormatter(Stream stream)
        {
            return new NewtonsoftJsonFormatter(this, stream);
        }
    }
}
