using System;
using System.Threading.Tasks;

namespace PolyMessage.Tests.Micro.ILEmitterTests.IL
{
    // classes here are used only to see what IL the compiler generates

    public static class StaticType
    {
        public static Task CastToTaskOfResponse(int responseTypeID, Task<object> taskOfObject)
        {
            switch (responseTypeID)
            {
                case 123:
                    return CastPlaceHolder.GenericCast<Response1>(taskOfObject);
                case 234:
                    return CastPlaceHolder.GenericCast<Response2>(taskOfObject);
                case 345:
                    return CastPlaceHolder.GenericCast<Response3>(taskOfObject);
                case 456:
                    return CastPlaceHolder.GenericCast<Response4>(taskOfObject);
                case 567:
                    return CastPlaceHolder.GenericCast<Response5>(taskOfObject);
                case 678:
                    return CastPlaceHolder.GenericCast<Response6>(taskOfObject);
                case 789:
                    return CastPlaceHolder.GenericCast<Response7>(taskOfObject);
                default:
                    throw new InvalidOperationException($"Unknown response type ID {responseTypeID}.");
            }
        }

        public static Task<object> Dispatch(int responseTypeID, object request, object implementor)
        {
            switch (responseTypeID)
            {
                case 123:
                    Task<Response1> responseTask1 = ((IContract1)implementor).Operation1((Request1)request);
                    return CastPlaceHolder.GenericCast(responseTask1);
                case 456:
                    Task<Response5> responseTask5 = ((IContract2)implementor).Operation5((Request5)request);
                    return CastPlaceHolder.GenericCast(responseTask5);
                default:
                    throw new InvalidOperationException($"Unknown response type ID {responseTypeID}.");
            }
        }
    }

    public static class CastPlaceHolder
    {
        public static async Task<TResponse> GenericCast<TResponse>(Task<object> taskOfObject)
        {
            return (TResponse)await taskOfObject.ConfigureAwait(false);
        }

        public static async Task<object> GenericCast<TResponse>(Task<TResponse> taskOfResponse)
        {
            return await taskOfResponse.ConfigureAwait(false);
        }
    }

    public interface IContract1
    {
        Task<Response1> Operation1(Request1 request);
        Task<Response2> Operation2(Request2 request);
        Task<Response3> Operation3(Request3 request);
        Task<Response4> Operation4(Request4 request);
    }

    public interface IContract2
    {
        Task<Response5> Operation5(Request5 request);
        Task<Response6> Operation6(Request6 request);
        Task<Response7> Operation7(Request7 request);
    }

    public sealed class Request1 { }
    public sealed class Response1 { }
    public sealed class Request2 { }
    public sealed class Response2 { }
    public sealed class Request3 { }
    public sealed class Response3 { }
    public sealed class Request4 { }
    public sealed class Response4 { }
    public sealed class Request5 { }
    public sealed class Response5 { }
    public sealed class Request6 { }
    public sealed class Response6 { }
    public sealed class Request7 { }
    public sealed class Response7 { }
}
