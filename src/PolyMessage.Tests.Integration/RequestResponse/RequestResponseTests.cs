using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.RequestResponse
{
    public abstract class RequestResponseTests : IntegrationFixture
    {
        protected RequestResponseTests(ITestOutputHelper output) : base(output, services =>
        {
            services.AddScoped<ISingleOperationContract, SingleOperationImplementor>();
            services.AddScoped<IMultipleOperationsContract, MultipleOperationsImplementor>();
        })
        {}

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async Task SingleOperationContract(int messagesCount)
        {
            // arrange
            Host.AddContract<ISingleOperationContract>();
            Client.AddContract<ISingleOperationContract>();

            // act & assert
            await StartHostAndConnectClient();
            ISingleOperationContract proxy = Client.Get<ISingleOperationContract>();
            const string request = "request";

            using (new AssertionScope())
            {
                for (int i = 0; i < messagesCount; ++i)
                {
                    SingleOperationResponse response = await proxy.Operation(new SingleOperationRequest{Data = request});
                    response.Data.Should().Be("response");
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async Task MultipleOperationsContract(int messagesCount)
        {
            // arrange
            Host.AddContract<IMultipleOperationsContract>();
            Client.AddContract<IMultipleOperationsContract>();

            // act & assert
            await StartHostAndConnectClient();
            IMultipleOperationsContract proxy = Client.Get<IMultipleOperationsContract>();
            const string request = "request";

            using (new AssertionScope())
            {
                for (int i = 0; i < messagesCount; ++i)
                {
                    MultipleOperationsResponse1 response1 = await proxy.Operation1(new MultipleOperationsRequest1 {Data = request});
                    MultipleOperationsResponse2 response2 = await proxy.Operation2(new MultipleOperationsRequest2 {Data = request});
                    MultipleOperationsResponse3 response3 = await proxy.Operation3(new MultipleOperationsRequest3 {Data = request});

                    response1.Data.Should().Be("response1");
                    response2.Data.Should().Be("response2");
                    response3.Data.Should().Be("response3");
                }
            }
        }
    }
}
