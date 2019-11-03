using System.Threading.Tasks;

namespace PolyMessage.IntegrationTests.RequestResponse
{
    [PolyContract]
    public interface IStringContract
    {
        [PolyRequestResponseEndpoint]
        Task<string> Call1(string request);

        [PolyRequestResponseEndpoint]
        Task<string> Call2(string request);
    }

    public sealed class StringImplementor : IStringContract
    {
        public Task<string> Call1(string request)
        {
            return Task.FromResult(request);
        }

        public Task<string> Call2(string request)
        {
            return Task.FromResult(request);
        }
    }
}
