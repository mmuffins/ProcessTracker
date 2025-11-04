namespace ProcessTrackerService.Core.Interfaces;

public interface IDateTimeProvider
{
    DateTime Now { get; }
}
