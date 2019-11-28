using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Format
{
    public abstract class DataTests : IntegrationFixture
    {
        private readonly DataImplementor _implementorInstance;

        protected DataTests(ITestOutputHelper output) : base(output, services =>
        {
            services.AddSingleton<IDataContract, DataImplementor>();
        })
        {
            _implementorInstance = (DataImplementor)ServiceProvider.GetRequiredService<IDataContract>();

            Client.AddContract<IDataContract>();
            Host.AddContract<IDataContract>();
        }

        [Theory] // size = encoding bytes per char x
        [InlineData(1024)] // 1KB
        [InlineData(1048576)] // 1MB
        public async Task LargeString(int stringLength)
        {
            // arrange
            StringBuilder builder = new StringBuilder();
            DateTime utcNow = DateTime.UtcNow;
            while (builder.Length < stringLength)
            {
                builder.Append(utcNow);
            }
            string largeString = builder.ToString();

            // act
            await StartHostAndConnectClient();
            await Client.Get<IDataContract>().LargeString(new LargeStringRequest {LargeString = largeString});

            // assert
            _implementorInstance.LastLargeStringRequest.LargeString.Should().Be(largeString);
        }

        [Theory]
        [InlineData(1024)]
        public async Task LargeNumberOfObjects(int objectsCount)
        {
            // arrange
            LargeNumberOfObjectsRequest request = new LargeNumberOfObjectsRequest();
            for (int i = 0; i < objectsCount; ++i)
            {
                request.Objects.Add(new Object {Data = "data"});
            }

            // act
            await StartHostAndConnectClient();
            await Client.Get<IDataContract>().LargeNumberOfObjects(request);

            // assert
            _implementorInstance.LastLargeNumberOfObjectsRequest.Should().BeEquivalentTo(request);
        }

        [Theory]
        [InlineData(1024)] // 1KB
        public async Task LargeArrays(int arrayLength)
        {
            // arrange
            byte[] largeArray = new byte[arrayLength];
            Random r = new Random();
            for (int i = 0; i < largeArray.Length; ++i)
            {
                largeArray[i] = (byte) r.Next(0, byte.MaxValue);
            }

            // act
            await StartHostAndConnectClient();
            await Client.Get<IDataContract>().LargeArray(new LargeArrayRequest {LargeArray = largeArray});

            // assert
            _implementorInstance.LastLargeArrayRequest.LargeArray.Should().BeEquivalentTo(largeArray);
        }
    }
}
