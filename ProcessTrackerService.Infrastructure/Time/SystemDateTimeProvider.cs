using System;
using ProcessTrackerService.Core.Interfaces;

namespace ProcessTrackerService.Infrastructure.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now;
}
