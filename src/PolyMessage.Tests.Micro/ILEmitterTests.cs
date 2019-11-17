using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using PolyMessage.CodeGeneration;
using PolyMessage.Metadata;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Micro
{
    public class ILEmitterTests : BaseFixture
    {
        private readonly CastTaskOfObjectToTaskOfResponse _toResponseTaskCast;
        private readonly CastTaskOfResponseToTaskOfObject _toObjectTaskCast;

        public ILEmitterTests(ITestOutputHelper output) : base(output)
        {
            ICodeGenerator target = new ILEmitter();
            target.GenerateCode(new List<Operation>
            {
                new Operation {ResponseID = 456, ResponseType = typeof(Response1)},
                new Operation {ResponseID = 123, ResponseType = typeof(Response2)}
            });
            _toResponseTaskCast = target.GetCastTaskOfObjectToTaskOfResponseDelegate();
            _toObjectTaskCast = target.GetCastTaskOfResponseToTaskOfObjectDelegate();
        }

        [Fact]
        public void ToResponseTaskCastingShouldSucceed()
        {
            // arrange
            Response1 response1 = new Response1();
            Response2 response2 = new Response2();

            // act
            Task<Response1> response1Task = (Task<Response1>) _toResponseTaskCast(456, Task.Run(() => (object) response1));
            Task<Response2> response2Task = (Task<Response2>) _toResponseTaskCast(123, Task.Run(() => (object) response2));

            response1Task.Wait();
            response2Task.Wait();

            // assert
            using (new AssertionScope())
            {
                response1Task.IsCompletedSuccessfully.Should().BeTrue();
                response1Task.Exception.Should().BeNull();
                response1Task.Result.Should().BeSameAs(response1);

                response2Task.IsCompletedSuccessfully.Should().BeTrue();
                response2Task.Exception.Should().BeNull();
                response2Task.Result.Should().BeSameAs(response2);
            }
        }

        [Fact]
        public void ToResponseTaskCastingShouldThrowWhenMessageIsUnknown()
        {
            // arrange
            int unknownMessageID = -1;

            // act
            Action act = () => _toResponseTaskCast(unknownMessageID, Task.FromResult((object) new Response1())).Wait();

            // assert
            act.Should().Throw<InvalidOperationException>().WithMessage($"*{unknownMessageID}*");
        }

        [Fact]
        public void ToObjectTaskCastingShouldSucceed()
        {
            // arrange
            Response1 response1 = new Response1();
            Response2 response2 = new Response2();

            // act
            Task<object> response1Task = _toObjectTaskCast(456, Task.Run(() => response1));
            Task<object> response2Task = _toObjectTaskCast(123, Task.Run(() => response2));

            response1Task.Wait();
            response2Task.Wait();

            // assert
            using (new AssertionScope())
            {
                response1Task.IsCompletedSuccessfully.Should().BeTrue();
                response1Task.Exception.Should().BeNull();
                response1Task.Result.Should().BeSameAs(response1);

                response2Task.IsCompletedSuccessfully.Should().BeTrue();
                response2Task.Exception.Should().BeNull();
                response2Task.Result.Should().BeSameAs(response2);
            }
        }

        [Fact]
        public void ToObjectTaskCastingShouldThrowWhenMessageIsUnknown()
        {
            // arrange
            int unknownMessageID = -1;

            // act
            Action act = () => _toObjectTaskCast(unknownMessageID, Task.FromResult(new Response1())).Wait();

            // assert
            act.Should().Throw<InvalidOperationException>().WithMessage($"*{unknownMessageID}*");
        }
    }

    public static class UsedJustToSeeGeneratedIL
    {
        public static Task CastTaskOfObjectToTaskOfResponse(int messageID, Task<object> taskOfObject)
        {
            switch (messageID)
            {
                case 123: return CastPlaceHolder.GenericCast<Response1>(taskOfObject);
                case 234: return CastPlaceHolder.GenericCast<Response2>(taskOfObject);
                case 345: return CastPlaceHolder.GenericCast<Response3>(taskOfObject);
                case 456: return CastPlaceHolder.GenericCast<Response4>(taskOfObject);
                case 567: return CastPlaceHolder.GenericCast<Response5>(taskOfObject);
                case 678: return CastPlaceHolder.GenericCast<Response6>(taskOfObject);
                case 789: return CastPlaceHolder.GenericCast<Response7>(taskOfObject);
                default: throw new InvalidOperationException($"Unknown message ID {messageID}.");
            }
        }

        public static Task<object> CastTaskOfResponseToTaskOfObject(int messageID, Task taskOfResponse)
        {
            switch (messageID)
            {
                case 123: return CastPlaceHolder.GenericCast((Task<Response1>) taskOfResponse);
                case 234: return CastPlaceHolder.GenericCast((Task<Response2>) taskOfResponse);
                case 345: return CastPlaceHolder.GenericCast((Task<Response3>) taskOfResponse);
                case 456: return CastPlaceHolder.GenericCast((Task<Response4>) taskOfResponse);
                case 567: return CastPlaceHolder.GenericCast((Task<Response5>) taskOfResponse);
                case 678: return CastPlaceHolder.GenericCast((Task<Response6>) taskOfResponse);
                case 789: return CastPlaceHolder.GenericCast((Task<Response7>) taskOfResponse);
                default: throw new InvalidOperationException($"Unknown message ID {messageID}.");
            }
        }
    }

    public static class CastPlaceHolder
    {
        public static async Task<TResponse> GenericCast<TResponse>(Task<object> taskOfObject)
        {
            return (TResponse) await taskOfObject.ConfigureAwait(false);
        }

        public static async Task<object> GenericCast<TResponse>(Task<TResponse> taskOfResponse)
        {
            return await taskOfResponse.ConfigureAwait(false);
        }
    }

    public sealed class Request1 {}
    public sealed class Response1 {}
    public sealed class Request2 {}
    public sealed class Response2 {}
    public sealed class Request3 {}
    public sealed class Response3 {}
    public sealed class Request4 {}
    public sealed class Response4 {}
    public sealed class Request5 {}
    public sealed class Response5 {}
    public sealed class Request6 {}
    public sealed class Response6 {}
    public sealed class Request7 {}
    public sealed class Response7 {}
}
