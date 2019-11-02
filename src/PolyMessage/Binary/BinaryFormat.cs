using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage.Binary
{
    public class BinaryFormat : IFormat
    {
        private readonly BinaryFormatter _formatter;

        public BinaryFormat()
        {
            _formatter = new BinaryFormatter();
        }

        public string DisplayName => "Binary";

        public Task WriteToStream(string message, Stream stream, CancellationToken cancelToken)
        {
            _formatter.Serialize(stream, message);
            return Task.CompletedTask;
        }

        public Task<string> ReadFromStream(Stream stream, CancellationToken cancelToken)
        {
            string message = (string) _formatter.Deserialize(stream);
            return Task.FromResult(message);
        }
    }
}
