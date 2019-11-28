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

namespace PolyMessage.Tests.Integration.Performance
{
    public abstract class PerformanceTests : IntegrationFixture
    {
        protected PerformanceTests(ITestOutputHelper output) : base(output, services =>
        {
            services.AddScoped<IPerformanceContract, PerformanceImplementor>();
        })
        {}

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 100)]
        [InlineData(4, 1)]
        [InlineData(4, 100)]
        [InlineData(8, 100)]
        public async Task SendMessagesFast(int clientsCount, int messagesCount)
        {
            // arrange
            for (int i = 0; i < clientsCount; ++i)
            {
                Clients.Add(CreateClient());
            }

            // act
            Host.AddContract<IPerformanceContract>();
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
            client.AddContract<IPerformanceContract>();
            await client.ConnectAsync();
            IPerformanceContract proxy = client.Get<IPerformanceContract>();

            // warmup
            PerformanceRequest1 request = new PerformanceRequest1 {Data = "request"};
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
