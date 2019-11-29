using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage.Tests.Integration.ImplementorProvision
{
    [PolyContract]
    public interface IDisposableContract : IDisposable
    {
        [PolyRequestResponse]
        Task<Response1> Operation1(Request1 request);

        [PolyRequestResponse]
        Task<Response2> Operation2(Request2 request2);
    }

    public class DisposableImplementor : IDisposableContract
    {
        private static int _disposedCount;
        public static int DisposedCount => _disposedCount;

        public static void ResetDisposedCount()
        {
            _disposedCount = 0;
        }

        public void Dispose()
        {
            Interlocked.Increment(ref _disposedCount);
        }

        public Task<Response1> Operation1(Request1 request)
        {
            return Task.FromResult(new Response1());
        }

        public Task<Response2> Operation2(Request2 request2)
        {
            return Task.FromResult(new Response2());
        }
    }

    [PolyMessage, Serializable, DataContract]
    public sealed class Request1 {}

    [PolyMessage, Serializable, DataContract]
    public sealed class Request2 {}

    [PolyMessage, Serializable, DataContract]
    public sealed class Response1 {}

    [PolyMessage, Serializable, DataContract]
    public sealed class Response2 {}
}
