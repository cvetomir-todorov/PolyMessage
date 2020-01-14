using System;
using System.IO;

namespace PolyMessage.Formats.NewtonsoftJson
{
    public class NewtonsoftJsonFormat : PolyFormat
    {
        public override string DisplayName => "NewtonsoftJSON";

        public override PolyFormatter CreateFormatter(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            return new NewtonsoftJsonFormatter(this, stream);
        }
    }
}
