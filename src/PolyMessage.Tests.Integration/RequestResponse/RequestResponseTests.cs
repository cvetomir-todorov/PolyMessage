using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        [InlineData(100)]
        [InlineData(1000)]
        public async Task SingleOperationContract(int messagesCount)
        {
            // arrange
            Client = CreateClient();

            // act & assert
            Host.AddContract<ISingleOperationContract>();
            await StartHost();

            Client.AddContract<ISingleOperationContract>();
            Client.Connect();
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
        [InlineData(100)]
        [InlineData(1000)]
        public async Task MultipleOperationsContract(int messagesCount)
        {
            // arrange
            Client = CreateClient();

            // act & assert
            Host.AddContract<IMultipleOperationsContract>();
            await StartHost();

            Client.AddContract<IMultipleOperationsContract>();
            Client.Connect();
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

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 100)]
        [InlineData(2, 1)]
        [InlineData(2, 100)]
        [InlineData(10, 100)]
        public async Task SendMessagesFast(int clientsCount, int messagesCount)
        {
            // arrange
            for (int i = 0; i < clientsCount; ++i)
            {
                PolyClient client = CreateClient();
                Clients.Add(client);
            }

            // act
            Host.AddContract<IMultipleOperationsContract>();
            await StartHost();

            List<Task<TimeSpan>> clientTasks = new List<Task<TimeSpan>>();
            foreach (PolyClient client in Clients)
            {
                Task<TimeSpan> clientTask = Task.Run(async () =>
                {
                    TimeSpan duration = await MakeRequests(client, messagesCount);
                    Logger.LogInformation("Making {0} requests from a client took: {1:0} ms.", messagesCount, duration.TotalMilliseconds);
                    return duration;
                });
                clientTasks.Add(clientTask);
            }

            Task.WaitAll(clientTasks.ToArray(), TimeSpan.FromSeconds(10));

            // assert
            using (new AssertionScope())
            {
                int succeededTasks = clientTasks.Count(ct => ct.IsCompletedSuccessfully);
                succeededTasks.Should().Be(clientTasks.Count);

                foreach (Task<TimeSpan> clientTask in clientTasks)
                {
                    clientTask.Exception.Should().BeNull();
                    TimeSpan totalDuration = clientTask.Result;
                    TimeSpan durationPerRequest = totalDuration / messagesCount;
                    Logger.LogInformation("Duration per request: {0}", durationPerRequest);
                    durationPerRequest.Should().BeLessOrEqualTo(TimeSpan.FromMilliseconds(5.0));
                }
            }
        }

        private async Task<TimeSpan> MakeRequests(PolyClient client, int messagesCount)
        {
            client.AddContract<IMultipleOperationsContract>();
            client.Connect();
            IMultipleOperationsContract proxy = client.Get<IMultipleOperationsContract>();

            // warmup
            MultipleOperationsRequest1 request = new MultipleOperationsRequest1{Data = "request"};
            await proxy.Operation1(request);

            Stopwatch requestsWatch = Stopwatch.StartNew();
            for (int i = 0; i < messagesCount; ++i)
            {
                await proxy.Operation1(request);
            }

            requestsWatch.Stop();
            return requestsWatch.Elapsed;
        }
    }
}
