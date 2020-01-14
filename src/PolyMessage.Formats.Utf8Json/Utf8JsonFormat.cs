using System;
using System.IO;

namespace PolyMessage.Formats.Utf8Json
{
    public class Utf8JsonFormat : PolyFormat
    {
        public override string DisplayName => "Utf8Json";

        public override PolyFormatter CreateFormatter(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            return new Utf8JsonFormatter(this, stream);
        }
    }
}
