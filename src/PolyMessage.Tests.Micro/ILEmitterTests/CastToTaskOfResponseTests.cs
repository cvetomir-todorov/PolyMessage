using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using PolyMessage.CodeGeneration;
using PolyMessage.Metadata;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Micro.ILEmitterTests
{
    public class CastToTaskOfResponseTests : BaseFixture
    {
        private readonly CastToTaskOfResponse _castToTaskOfResponse;

        public CastToTaskOfResponseTests(ITestOutputHelper output) : base(output)
        {
            ICodeGenerator target = new ILEmitter();

            Type contractType = typeof(IContract);
            MethodInfo methodAlpha = contractType.GetMethod(nameof(IContract.Alpha));
            MethodInfo methodBeta = contractType.GetMethod(nameof(IContract.Beta));
            target.GenerateCode(new List<Operation>
            {
                new Operation
                {
                    ResponseTypeID = 456, ResponseType = typeof(ResponseAlpha), RequestTypeID = 567, RequestType = typeof(RequestAlpha),
                    ContractType = contractType, Method = methodAlpha
                },
                new Operation
                {
                    ResponseTypeID = 123, ResponseType = typeof(ResponseBeta), RequestTypeID = 234, RequestType = typeof(RequestBeta),
                    ContractType = contractType, Method = methodBeta
                }
            });
            _castToTaskOfResponse = target.GetCastToTaskOfResponse();
        }

        [Fact]
        public void ShouldSucceed()
        {
            // arrange
            ResponseAlpha alphaResponse = new ResponseAlpha();
            ResponseBeta betaResponse = new ResponseBeta();

            // act
            Task<ResponseAlpha> alphaTask = (Task<ResponseAlpha>) _castToTaskOfResponse(456, Task.Run(() => (object) alphaResponse));
            Task<ResponseBeta> betaTask = (Task<ResponseBeta>) _castToTaskOfResponse(123, Task.Run(() => (object) betaResponse));

            alphaTask.Wait();
            betaTask.Wait();

            // assert
            using (new AssertionScope())
            {
                alphaTask.IsCompletedSuccessfully.Should().BeTrue();
                alphaTask.Exception.Should().BeNull();
                alphaTask.Result.Should().BeSameAs(alphaResponse);

                betaTask.IsCompletedSuccessfully.Should().BeTrue();
                betaTask.Exception.Should().BeNull();
                betaTask.Result.Should().BeSameAs(betaResponse);
            }
        }

        [Fact]
        public void ShouldThrowWhenResponseIsUnknown()
        {
            // arrange
            short unknownResponseTypeID = -1;

            // act
            Action act = () => _castToTaskOfResponse(unknownResponseTypeID, Task.FromResult((object) new ResponseAlpha())).Wait();

            // assert
            act.Should().Throw<InvalidOperationException>().WithMessage($"*{unknownResponseTypeID}*");
        }

        public interface IContract
        {
            ResponseAlpha Alpha(RequestAlpha request);
            ResponseBeta Beta(RequestBeta request);
        }
        public class RequestAlpha {}
        public class ResponseAlpha {}
        public class RequestBeta {}
        public class ResponseBeta {}
    }

}
