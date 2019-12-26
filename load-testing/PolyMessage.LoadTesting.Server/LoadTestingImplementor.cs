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

        public Task<StringResponse> StringOperation(StringRequest request)
        {
            return Task.FromResult(new StringResponse {Data = request.Data});
        }

        public Task<ObjectsResponse> ObjectsOperation(ObjectsRequest request)
        {
            return Task.FromResult(new ObjectsResponse {Objects = request.Objects});
        }
    }
}
