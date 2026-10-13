// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectHttp.given;

internal sealed class a_manual_deadline : TimeProvider
{
    DeadlineTimer _timer = null!;

    internal TimeSpan DueTime { get; private set; }
    internal bool TimerWasDisposed => _timer.Disposed;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        DueTime = dueTime;
        _timer = new DeadlineTimer(callback, state);
        return _timer;
    }

    internal void Expire() => _timer.Fire();

    sealed class DeadlineTimer(TimerCallback callback, object? state) : ITimer
    {
        internal bool Disposed { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period) => !Disposed;
        public void Dispose() => Disposed = true;
        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }

        internal void Fire()
        {
            if (!Disposed)
            {
                callback(state);
            }
        }
    }
}
