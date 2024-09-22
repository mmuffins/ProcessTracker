using ProcessTrackerService.Core.Interfaces;

namespace ProcessTrackerService.Core.Entities
{
    public class TagSessionSummary : BaseEntity, IAggregateRoot
    {
        public int SummaryId { get; set; }
        public DateTime Day { get; set; }
        public int TagId { get; set; }
        public double Seconds { get; set; }
        public virtual Tag Tag { get; set; }
    }
}
