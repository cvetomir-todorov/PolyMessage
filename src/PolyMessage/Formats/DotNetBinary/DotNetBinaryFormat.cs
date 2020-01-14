using System;
using System.IO;

namespace PolyMessage.Formats.DotNetBinary
{
    public class DotNetBinaryFormat : PolyFormat
    {
        public override string DisplayName => "DotNetBinary";

        public override PolyFormatter CreateFormatter(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            return new DotNetBinaryFormatter(this, stream);
        }
    }
}
