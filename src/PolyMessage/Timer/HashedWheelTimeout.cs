﻿using System.Threading;
using System.Threading.Tasks;

namespace PolyMessage.Timer
{
    internal sealed class HashedWheelTimeout : ITimeout
    {
        internal const int StateInitial = 0;
        internal const int StateCancelled = 1;
        internal const int StateExpired = 2;

        private volatile int _state = StateInitial;
        internal int State => _state;

        internal readonly HashedWheelTimer _timer;
        private readonly ITimerTask _task;
        internal readonly long _deadline;

        // remainingRounds will be calculated and set by Worker.transferTimeoutsToBuckets() before the
        // HashedWheelTimeout will be added to the correct HashedWheelBucket.
        internal long _remainingRounds;

        internal HashedWheelTimeout _next;
        internal HashedWheelTimeout _prev;

        // The bucket to which the timeout was added
        internal HashedWheelBucket _bucket;

        internal HashedWheelTimeout(HashedWheelTimer timer, ITimerTask task, long deadline)
        {
            _timer = timer;
            _task = task;
            _deadline = deadline;
        }

        public ITimerTask TimerTask => _task;

        public bool Expired => _state == StateExpired;

        public bool Cancelled => _state == StateCancelled;

        public bool Cancel()
        {
            // only update the state it will be removed from HashedWheelBucket on next tick.
            if (!CompareAndSetState(StateInitial, StateCancelled))
            {
                return false;
            }
            // If a task should be canceled we put this to another queue which will be processed on each tick.
            // So this means that we will have a GC latency of max. 1 tick duration which is good enough. This way
            // we can make again use of our MpscLinkedQueue and so minimize the locking / overhead as much as possible.
            _timer._cancelledTimeouts.Enqueue(this);
            return true;
        }

        internal void Remove()
        {
            HashedWheelBucket bucket = _bucket;
            if (bucket != null)
            {
                bucket.Remove(this);
            }
            else
            {
                _timer.DecreasePendingTimeouts();
            }
        }

        internal void Expire()
        {
            if (!CompareAndSetState(StateInitial, StateExpired))
            {
                return;
            }

            Task.Run(() =>
            {
                _task.Run(this);
            });
        }

        private bool CompareAndSetState(int expected, int state)
        {
            int originalState = Interlocked.CompareExchange(ref _state, state, expected);
            return originalState == expected;
        }
    }
}
