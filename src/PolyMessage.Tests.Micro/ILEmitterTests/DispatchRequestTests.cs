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
    public class DispatchRequestTests : BaseFixture
    {
        private readonly DispatchRequest _dispatchRequest;

        public DispatchRequestTests(ITestOutputHelper output) : base(output)
        {
            ICodeGenerator target = new ILEmitter();

            Type contractAlpha = typeof(IContractAlpha);
            MethodInfo methodAlpha = contractAlpha.GetMethod(nameof(IContractAlpha.Alpha));

            Type contractBeta = typeof(IContractBeta);
            MethodInfo methodBeta = contractBeta.GetMethod(nameof(IContractBeta.Beta));

            target.GenerateCode(new List<Operation>
            {
                new Operation
                {
                    ResponseID = 456, ResponseType = typeof(ResponseAlpha), RequestID = 567, RequestType = typeof(RequestAlpha),
                    ContractType = contractAlpha, Method = methodAlpha
                },
                new Operation
                {
                    ResponseID = 123, ResponseType = typeof(ResponseBeta), RequestID = 234, RequestType = typeof(RequestBeta),
                    ContractType = contractBeta, Method = methodBeta
                }
            });

            _dispatchRequest = target.GetDispatchRequest();
        }

        [Fact]
        public void ShouldSucceed()
        {
            // arrange

            // act
            Task<object> alphaTask = _dispatchRequest(456, new RequestAlpha(), new ImplementorAlpha());
            Task<object> betaTask = _dispatchRequest(123, new RequestBeta(), new ImplementorBeta());

            alphaTask.Wait();
            betaTask.Wait();

            // assert
            using (new AssertionScope())
            {
                alphaTask.IsCompletedSuccessfully.Should().BeTrue();
                alphaTask.Exception.Should().BeNull();
                alphaTask.Result.Should().BeSameAs(ImplementorAlpha.Response);

                betaTask.IsCompletedSuccessfully.Should().BeTrue();
                betaTask.Exception.Should().BeNull();
                betaTask.Result.Should().BeSameAs(ImplementorBeta.Response);
            }
        }

        [Fact]
        public void ShouldThrowWhenMessageIsUnknown()
        {
            // arrange
            int unknownResponseID = -1;

            // act
            Action act = () => _dispatchRequest(unknownResponseID, new RequestAlpha(), new ImplementorAlpha()).Wait();

            // assert
            act.Should().Throw<InvalidOperationException>().WithMessage($"*{unknownResponseID}*");
        }

        public interface IContractAlpha
        {
            Task<ResponseAlpha> Alpha(RequestAlpha request);
        }
        public class ImplementorAlpha : IContractAlpha
        {
            public static readonly ResponseAlpha Response = new ResponseAlpha();
            public Task<ResponseAlpha> Alpha(RequestAlpha request) => Task.FromResult(Response);
        }
        public interface IContractBeta
        {
            Task<ResponseBeta> Beta(RequestBeta request);
        }
        public class ImplementorBeta : IContractBeta
        {
            public static readonly ResponseBeta Response = new ResponseBeta();
            public Task<ResponseBeta> Beta(RequestBeta request) => Task.FromResult(Response);
        }
        public class RequestAlpha { }
        public class ResponseAlpha { }
        public class RequestBeta { }
        public class ResponseBeta { }
    }
}
