using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.IntegrationTests.RequestResponse
{
    public class Tests : BaseFixture
    {
        public Tests(ITestOutputHelper output) : base(output)
        {}

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 100)]
        [InlineData(2, 1)]
        [InlineData(2, 100)]
        [InlineData(10, 100)]
        public async Task ShouldCommunicate(int clientsCount, int messagesCount)
        {
            // arrange
            for (int i = 0; i < clientsCount; ++i)
            {
                PolyClient client = CreateClient(ServerAddress, ServiceProvider);
                Clients.Add(client);
            }

            // act
            Host.AddContract<IStringContract, StringImplementor>();
            await StartHost();

            List<Task<double>> clientTasks = new List<Task<double>>();
            foreach (PolyClient client in Clients)
            {
                Task<double> clientTask = Task.Run(async () =>
                {
                    TimeSpan duration = await MakeRequests(client, messagesCount);
                    Logger.LogInformation("Making {0} requests from a client took: {1:0} ms.", messagesCount, duration.TotalMilliseconds);
                    return duration.TotalMilliseconds;
                });
                clientTasks.Add(clientTask);
            }

            Task.WaitAll(clientTasks.ToArray(), TimeSpan.FromSeconds(10));

            // assert
            using (new AssertionScope())
            {
                int succeededTasks = clientTasks.Count(ct => ct.IsCompletedSuccessfully);
                succeededTasks.Should().Be(clientTasks.Count);

                foreach (Task<double> clientTask in clientTasks)
                {
                    clientTask.Exception.Should().BeNull();
                    double totalDuration = clientTask.Result;
                    double durationPerRequest = totalDuration / messagesCount;
                    durationPerRequest.Should().BeLessOrEqualTo(1.0);
                }
            }
        }

        private async Task<TimeSpan> MakeRequests(PolyClient client, int messagesCount)
        {
            // currently this connects to the server
            client.AddContract<IStringContract>();
            IStringContract proxy = client.Get<IStringContract>();

            // warmup
            const string requestMessage = "request";
            await proxy.Call1(requestMessage);

            Stopwatch requestsWatch = Stopwatch.StartNew();
            for (int i = 0; i < messagesCount; ++i)
            {
                await proxy.Call1(requestMessage);
            }

            requestsWatch.Stop();
            return requestsWatch.Elapsed;
        }
    }
}
