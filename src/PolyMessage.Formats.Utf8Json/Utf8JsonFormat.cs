using System.IO;

namespace PolyMessage.Formats.Utf8Json
{
    public class Utf8JsonFormat : PolyFormat
    {
        public override string DisplayName => "Utf8Json";

        // TODO: validate input

        public override PolyFormatter CreateFormatter(Stream stream)
        {
            return new Utf8JsonFormatter(this, stream);
        }
    }
}
