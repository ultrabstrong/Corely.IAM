namespace Corely.IAM.Web.UnitTests.Helpers;

public sealed class ManualTimeProvider : TimeProvider
{
    private readonly List<ManualTimer> _timers = [];

    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period
    )
    {
        var timer = new ManualTimer(() => callback(state));
        _timers.Add(timer);
        return timer;
    }

    public void FireTimers()
    {
        foreach (var timer in _timers.ToList())
            timer.Fire();
    }

    private sealed class ManualTimer(Action callback) : ITimer
    {
        private bool _disposed;

        public void Fire()
        {
            if (!_disposed)
                callback();
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose() => _disposed = true;

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
