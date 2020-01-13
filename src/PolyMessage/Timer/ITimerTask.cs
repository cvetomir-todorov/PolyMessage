namespace PolyMessage.Timer
{
    /// <summary>
    /// A task which is executed after the delay specified with ITimer.NewTimeout(ITimerTask, long, TimeUnit).
    /// </summary>
    internal interface ITimerTask
    {
        /// <summary>
        /// Executed after the delay specified with ITimer.NewTimeout(ITimerTask, long, TimeUnit)
        /// </summary>
        /// <param name="timeout">timeout a handle which is associated with this task</param>
        void Run(ITimeout timeout);
    }
}
