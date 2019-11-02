using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage.Formats
{
    public interface IFormat
    {
        string DisplayName { get; }

        Task WriteToStream(string message, Stream stream, CancellationToken cancelToken);

        Task<string> ReadFromStream(Stream stream, CancellationToken cancelToken);
    }
}
