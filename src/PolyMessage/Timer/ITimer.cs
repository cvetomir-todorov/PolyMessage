using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PolyMessage.Timer
{
    internal interface ITimer : IDisposable
    {
        ITimeout NewTimeout(ITimerTask task, TimeSpan span);

        ISet<ITimeout> Stop();

        INotifyCompletion Delay(TimeSpan interval);
    }
}
