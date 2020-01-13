using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace PolyMessage.Timer
{
    /// <summary>
    /// A ITimer optimized for approximated I/O timeout scheduling.
    /// 
    /// ## Tick Duration ##
    /// As described with 'approximated', this timer does not execute the scheduled ITimerTask on time. HashedWheelTimer, 
    /// on every tick, will check if there are any TimerTasks behind the schedule and execute them.
    /// You can increase or decrease the accuracy of the execution timing by specifying smaller or larger tick duration 
    /// in the constructor.In most network applications, I/O timeout does not need to be accurate. 
    /// Therefore, the default tick duration is 100 milliseconds and you will not need to try different configurations in most cases.
    /// 
    /// ## Ticks per Wheel (Wheel Size) ##
    /// 
    /// HashedWheelTimer maintains a data structure called 'wheel'. 
    /// To put simply, a wheel is a hash table of TimerTasks whose hash function is 'dead line of the task'. 
    /// The default number of ticks per wheel (i.e. the size of the wheel) is 512. 
    /// You could specify a larger value if you are going to schedule a lot of timeouts.
    /// 
    /// ## Do not create many instances. ##
    /// 
    /// HashedWheelTimer creates a new thread whenever it is instantiated and started. 
    /// Therefore, you should make sure to create only one instance and share it across your application. 
    /// One of the common mistakes, that makes your application unresponsive, is to create a new instance for every connection.
    /// 
    /// ## Implementation Details ##
    /// HashedWheelTimer is based on George Varghese and Tony Lauck's paper, 
    /// 'Hashed and Hierarchical Timing Wheels: data structures to efficiently implement a timer facility'. 
    /// More comprehensive slides are located here http://www.cse.wustl.edu/~cdgill/courses/cs6874/TimingWheels.ppt.
    /// </summary>
    internal sealed class HashedWheelTimer : ITimer
    {
        internal const int WorkerStateInit = 0;
        internal const int WorkerStateStarted = 1;
        internal const int WorkerStateShutdown = 2;

        private volatile int _workerState; // 0 - init, 1 - started, 2 - shut down

        private readonly long _tickDuration;
        private readonly HashedWheelBucket[] _wheel;
        private readonly int _mask;
        private readonly ManualResetEvent _startTimeInitialized = new ManualResetEvent(false);
        private readonly ConcurrentQueue<HashedWheelTimeout> _timeouts = new ConcurrentQueue<HashedWheelTimeout>();
        internal readonly ConcurrentQueue<HashedWheelTimeout> _cancelledTimeouts = new ConcurrentQueue<HashedWheelTimeout>();
        private readonly long _maxPendingTimeouts;
        private readonly Thread _workerThread;

        /// <summary>
        /// There are 10,000 ticks in a millisecond
        /// </summary>
        private readonly long _base = DateTime.UtcNow.Ticks / 10000;

        private /*volatile*/ long _startTime;
        private long _pendingTimeouts;

        private readonly ISet<ITimeout> _unprocessedTimeouts = new HashSet<ITimeout>();
        private long _tick;
        private readonly ILogger _logger;
        private bool _isDisposed;

        private long GetCurrentMs() { return DateTime.UtcNow.Ticks / 10000 - _base; }

        internal long DecreasePendingTimeouts()
        {
            return Interlocked.Decrement(ref _pendingTimeouts);
        }

        /// <summary>
        /// Creates a new timer with default tick duration 100 ms, and default number of ticks per wheel 512.
        /// </summary>
        public HashedWheelTimer(ILogger logger) : this(logger, TimeSpan.FromMilliseconds(100), 512, maxPendingTimeouts: long.MaxValue)
        {}

        /// <summary>
        /// Creates a new timer.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="tickDuration">the duration between tick</param>
        /// <param name="ticksPerWheel">the size of the wheel</param>
        /// <param name="maxPendingTimeouts">The maximum number of pending timeouts after which call to NewTimeout will result in InvalidOperationException being thrown. No maximum pending timeouts limit is assumed if this value is 0 or negative.
        /// </param>
        public HashedWheelTimer(ILogger logger, TimeSpan tickDuration, int ticksPerWheel, long maxPendingTimeouts)
        {
            if (tickDuration.TotalMilliseconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tickDuration), "must be greater than 0 ms");
            }
            if (ticksPerWheel <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ticksPerWheel), "must be greater than 0: ");
            }

            _logger = logger;

            // Normalize ticksPerWheel to power of two and initialize the wheel.
            _wheel = CreateWheel(ticksPerWheel);
            _mask = _wheel.Length - 1;

            // Convert tickDuration to ms.
            _tickDuration = (long)tickDuration.TotalMilliseconds;

            // Prevent overflow.
            if (_tickDuration >= long.MaxValue / _wheel.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(tickDuration)
                    , $"{tickDuration} (expected: 0 < tickDuration in ms < {long.MaxValue / _wheel.Length}");
            }
            _workerThread = new Thread(Run);

            _maxPendingTimeouts = maxPendingTimeouts;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Stop();
                _startTimeInitialized.Dispose();
                _isDisposed = true;
            }
        }

        private static HashedWheelBucket[] CreateWheel(int ticksPerWheel)
        {
            if (ticksPerWheel <= 0 || ticksPerWheel > 1073741824)
            {
                throw new ArgumentOutOfRangeException(nameof(ticksPerWheel), "Ticks per wheel must be in (0, 2^30] range.");
            }

            ticksPerWheel = NormalizeTicksPerWheel(ticksPerWheel);
            HashedWheelBucket[] wheel = new HashedWheelBucket[ticksPerWheel];
            for (int i = 0; i < wheel.Length; i++)
            {
                wheel[i] = new HashedWheelBucket();
            }
            return wheel;
        }

        private static int NormalizeTicksPerWheel(int ticksPerWheel)
        {
            int normalizedTicksPerWheel = 1;
            while (normalizedTicksPerWheel < ticksPerWheel)
            {
                normalizedTicksPerWheel <<= 1;
            }
            return normalizedTicksPerWheel;
        }

        /// <summary>
        ///  Starts the background thread explicitly.  The background thread will start automatically on demand 
        ///  even if you did not call this method.
        /// </summary>
        private void Start()
        {
            switch (_workerState)
            {
                case WorkerStateInit:
                    int originalWorkerState = Interlocked.CompareExchange(ref _workerState, WorkerStateStarted, WorkerStateInit);
                    if (originalWorkerState == WorkerStateInit)
                    {
                        _workerThread.Start();
                    }
                    break;
                case WorkerStateStarted:
                    break;
                case WorkerStateShutdown:
                    return;
                default:
                    throw new InvalidOperationException("HashedWheelTimer.workerState is invalid");
            }

            // Wait until the startTime is initialized by the worker.
            while (_startTime == 0)
            {
                bool started = _startTimeInitialized.WaitOne(TimeSpan.FromSeconds(5));
                if (!started)
                    throw new InvalidOperationException("Could not start.");
            }
        }

        /// <summary>
        /// Schedules the specified ITimerTask for one-time execution after the specified delay.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="span"></param>
        /// <returns>a handle which is associated with the specified task</returns>
        public ITimeout NewTimeout(ITimerTask task, TimeSpan span)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (_workerState == WorkerStateShutdown)
                return null;

            long pendingTimeoutsCount = Interlocked.Increment(ref _pendingTimeouts);

            if (pendingTimeoutsCount > _maxPendingTimeouts)
            {
                Interlocked.Decrement(ref _pendingTimeouts);
                throw new InvalidOperationException($"Number of pending timeouts ({pendingTimeoutsCount}) is greater than or equal to maximum allowed pending  timeouts ({_maxPendingTimeouts})");
            }

            Start();

            // Add the timeout to the timeout queue which will be processed on the next tick.
            // During processing all the queued HashedWheelTimeouts will be added to the correct HashedWheelBucket.
            long deadline = GetCurrentMs() + (long)span.TotalMilliseconds - _startTime;

            // Guard against overflow.
            if (span.TotalMilliseconds > 0 && deadline < 0)
            {
                deadline = long.MaxValue;
            }
            HashedWheelTimeout timeout = new HashedWheelTimeout(this, task, deadline);
            _timeouts.Enqueue(timeout);
            return timeout;
        }

        /// <summary>
        /// Releases all resources acquired by this ITimer and cancels all tasks which were scheduled but not executed yet.
        /// </summary>
        /// <returns></returns>
        public ISet<ITimeout> Stop()
        {
            int originalWorkerState = Interlocked.CompareExchange(ref _workerState, WorkerStateShutdown, WorkerStateStarted);
            
            if (originalWorkerState != WorkerStateStarted)
            {
                return new HashSet<ITimeout>();
            }

            int stopCounter = 10;
            while (stopCounter > 0)
            {
                if (_workerThread.Join(1000))
                {
                    _logger.LogInformation("Timer worker thread gracefully stopped.");
                    break;
                }
                stopCounter--;
            }

            if (_workerThread.IsAlive)
            {
                _logger.LogError("Timer worker thread was unable to stop gracefully. Trying to abort it...");
                _workerThread.Abort();
                if (!_workerThread.Join(1000))
                {
                    _logger.LogError("Timer worker thread was aborted but is still alive.");
                }
                else
                {
                    _logger.LogInformation("Timer worker thread successfully aborted.");
                }
            }

            return _unprocessedTimeouts;
        }

        private void Run()
        {
            // Initialize the startTime.
            _startTime = GetCurrentMs();
            if (_startTime == 0)
            {
                // We use 0 as an indicator for the uninitialized value here, so make sure it's not 0 when initialized.
                _startTime = 1;
            }

            // Notify the other threads waiting for the initialization at start().
            _startTimeInitialized.Set();

            do
            {
                long deadline = WaitForNextTick();
                if (deadline > 0)
                {
                    int idx = (int)(_tick & _mask);
                    ProcessCancelledTasks();
                    HashedWheelBucket bucket = _wheel[idx];
                    TransferTimeoutsToBuckets();
                    bucket.ExpireTimeouts(deadline);
                    _tick++;
                }
            } while (_workerState == WorkerStateStarted);

            // Fill the unprocessedTimeouts so we can return them from stop() method.
            foreach (HashedWheelBucket bucket in _wheel)
            {
                bucket.ClearTimeouts(_unprocessedTimeouts);
            }

            for (; ; )
            {
                HashedWheelTimeout timeout;
                if (!_timeouts.TryDequeue(out timeout) || timeout == null)
                {
                    break;
                }
                if (!timeout.Cancelled)
                {
                    _unprocessedTimeouts.Add(timeout);
                }
            }

            ProcessCancelledTasks();
        }

        private void TransferTimeoutsToBuckets()
        {
            // transfer only max. 100000 timeouts per tick to prevent a thread to stale the workerThread when it just
            // adds new timeouts in a loop.
            for (int i = 0; i < 100000; i++)
            {
                HashedWheelTimeout timeout;
                if (!_timeouts.TryDequeue(out timeout) || timeout == null)
                {
                    // all processed
                    break;
                }
                if (timeout.State == HashedWheelTimeout.StateCancelled)
                {
                    // Was cancelled in the meantime.
                    continue;
                }

                long calculated = timeout._deadline / _tickDuration;
                timeout._remainingRounds = (calculated - _tick) / _wheel.Length;

                long ticks = Math.Max(calculated, _tick); // Ensure we don't schedule for past.
                int stopIndex = (int)(ticks & _mask);

                HashedWheelBucket bucket = _wheel[stopIndex];
                bucket.AddTimeout(timeout);
            }
        }

        private void ProcessCancelledTasks()
        {
            int cancelledCount = 0;
            for (; ; )
            {
                if (!_cancelledTimeouts.TryDequeue(out HashedWheelTimeout timeout))
                {
                    // all processed
                    break;
                }

                if (timeout != null)
                {
                    timeout.Remove();
                    cancelledCount++;
                }
            }
        }

        private long WaitForNextTick()
        {
            long deadline = _tickDuration * (_tick + 1);

            for (; ; )
            {
                long currentTime = GetCurrentMs() - _startTime;
                int sleepTimeMs = (int)Math.Truncate(deadline - currentTime + 1M); 

                if (sleepTimeMs <= 0)
                {
                    if (currentTime == long.MaxValue)
                    {
                        return -long.MaxValue;
                    }
                    else
                    {
                        return currentTime;
                    }
                }

                Thread.Sleep(sleepTimeMs);
            }
        }

        public INotifyCompletion Delay(TimeSpan interval)
        {
            TimedAwaiter awaiter = new TimedAwaiter();
            NewTimeout(awaiter, interval);
            return awaiter;
        }
    }
}
