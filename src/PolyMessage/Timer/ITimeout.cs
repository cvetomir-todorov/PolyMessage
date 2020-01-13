
namespace PolyMessage.Timer
{
    /// <summary>
    /// A handle associated with a ITimerTask that is returned by a ITimer
    /// </summary>
    internal interface ITimeout
    {
        /// <summary>
        /// Returns the ITimerTask which is associated with this handle.
        /// </summary>
        ITimerTask TimerTask { get; }

        /// <summary>
        /// Returns true if and only if the ITimerTask associated
        /// with this handle has been expired
        /// </summary>
        bool Expired { get; }

        /// <summary>
        /// Returns true if and only if the ITimerTask associated
        /// with this handle has been cancelled
        /// </summary>
        bool Cancelled { get; }

        /// <summary>
        /// Attempts to cancel the {@link ITimerTask} associated with this handle.
        /// If the task has been executed or cancelled already, it will return with no side effect.
        /// </summary>
        /// <returns>True if the cancellation completed successfully, otherwise false</returns>
        bool Cancel();
    }
}
