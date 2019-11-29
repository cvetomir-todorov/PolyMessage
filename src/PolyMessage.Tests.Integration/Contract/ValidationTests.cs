using System;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using PolyMessage.Formats.DotNetBinary;
using PolyMessage.Transports.Tcp;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Contract
{
    public class ValidationTests : IntegrationFixture
    {
        public ValidationTests(ITestOutputHelper output) : base(output)
        {}

        protected override PolyFormat CreateFormat() => new DotNetBinaryFormat();
        protected override PolyTransport CreateTransport(Uri serverAddress) => new TcpTransport(serverAddress, LoggerFactory);

        [Theory]
        [InlineData("010", typeof(IContractWithoutAttribute), 1)]
        [InlineData("020", typeof(IContractWithoutOperations), 1)]
        [InlineData("030", typeof(IOperationWithoutAttribute), 1)]
        [InlineData("040", typeof(IOperationNotReturningTaskOfResponse), 1)]
        [InlineData("050", typeof(IOperationReturningResponseWithoutAttribute), 1)]
        [InlineData("060", typeof(IOperationAcceptingMoreThanOneRequest), 1)]
        [InlineData("070", typeof(IOperationAcceptingRequestWithoutAttribute), 1)]
        [InlineData("080", typeof(IMessagesWithSameTypeID), 1)]
        [InlineData("090", typeof(IOperationsWithSameRequests), 1)]
        [InlineData("100", typeof(IOperationsWithSameResponses), 1)]
        [InlineData("110", typeof(IOperationsWithSameMessagesAsRequestAndResponse), 1)]
        [InlineData("120", typeof(IMultipleErrors), 4)]
        public void ValidateContracts(string testOrder, Type contractType, int expectedValidationErrors)
        {
            // arrange

            // act
            Action serverAdd = () => Host.AddContract(contractType);
            Action clientAdd = () => Client.AddContract(contractType);

            // assert
            using (new AssertionScope())
            {
                PolyContractException serverException = serverAdd.Should().Throw<PolyContractException>().Which;
                if (serverException != null)
                {
                    serverException.ValidationErrors.Count.Should().Be(expectedValidationErrors);
                    Logger.LogInformation(serverException, "[Server]{0}", Environment.NewLine);
                }

                PolyContractException clientException = clientAdd.Should().Throw<PolyContractException>().Which;
                if (clientException != null)
                {
                    clientException.ValidationErrors.Count.Should().Be(expectedValidationErrors);
                    Logger.LogInformation(clientException, "[Client]{0}", Environment.NewLine);
                }
            }
        }
    }
}
