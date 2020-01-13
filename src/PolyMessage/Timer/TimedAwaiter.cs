using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PolyMessage.Timer
{
    internal sealed class TimedAwaiter : INotifyCompletion, ITimerTask
    {
        private Action _continuation;

        public void OnCompleted(Action continuation)
        {
            _continuation = continuation;
            if (IsCompleted)
                Interlocked.Exchange(ref _continuation, null)?.Invoke();
        }

        public bool IsCompleted { get; private set; }

        public void Run(ITimeout timeout)
        {
            IsCompleted = true;
            Interlocked.Exchange(ref _continuation, null)?.Invoke();
        }

        public TimedAwaiter GetAwaiter()
        {
            return this;
        }

        public object GetResult()
        {
            return null;
        }
    }
}
