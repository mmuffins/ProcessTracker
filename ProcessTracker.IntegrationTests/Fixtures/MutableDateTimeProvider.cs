using System;
using ProcessTrackerService.Core.Interfaces;

namespace ProcessTracker.IntegrationTests.Fixtures;

public sealed class MutableDateTimeProvider : IDateTimeProvider
{
    private readonly object _syncRoot = new();
    private DateTime _now = DateTime.UtcNow;

    public DateTime Now
    {
        get
        {
            lock (_syncRoot)
            {
                return _now;
            }
        }
    }

    public void Set(DateTime value)
    {
        lock (_syncRoot)
        {
            _now = value;
        }
    }

    public void Advance(TimeSpan value)
    {
        lock (_syncRoot)
        {
            _now = _now.Add(value);
        }
    }

    public void Reset()
    {
        lock (_syncRoot)
        {
            _now = DateTime.UtcNow;
        }
    }
}
