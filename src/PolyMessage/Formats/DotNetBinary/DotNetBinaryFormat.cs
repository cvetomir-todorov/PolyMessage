using System.IO;

namespace PolyMessage.Formats.DotNetBinary
{
    public class DotNetBinaryFormat : PolyFormat
    {
        public override string DisplayName => "DotNetBinary";

        public override PolyFormatter CreateFormatter(Stream stream)
        {
            return new DotNetBinaryFormatter(this, stream);
        }
    }
}
