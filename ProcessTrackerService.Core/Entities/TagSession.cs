using ProcessTrackerService.Core.Interfaces;

namespace ProcessTrackerService.Core.Entities
{
    public class TagSession : BaseEntity, IAggregateRoot
    {
        public int SessionId { get; set; }
        public int TagId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public DateTime? EndTime { get; set; }
        public virtual Tag Tag { get; set; }
    }
}
