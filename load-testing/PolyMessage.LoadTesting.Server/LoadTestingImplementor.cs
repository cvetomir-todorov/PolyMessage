using System.Threading.Tasks;
using PolyMessage.LoadTesting.Contracts;

namespace PolyMessage.LoadTesting.Server
{
    public class LoadTestingImplementor : ILoadTestingContract
    {
        private static readonly EmptyResponse _response = new EmptyResponse();

        public PolyConnection Connection { get; set; }

        public Task<EmptyResponse> EmptyOperation(EmptyRequest request)
        {
            return Task.FromResult(_response);
        }
    }
}
