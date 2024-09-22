using ProcessTrackerService.Core.Interfaces;

namespace ProcessTrackerService.Core.Entities
{
    public class Tag : BaseEntity, IAggregateRoot
    {
        public Tag()
        {
            Filters = new HashSet<Filter>();
            TagSessions = new HashSet<TagSession>();
            TagSessionSummaries = new HashSet<TagSessionSummary>();
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Inactive { get; set; }
        public virtual ICollection<Filter> Filters { get; set; }
        public virtual ICollection<TagSession> TagSessions { get; set; }
        public virtual ICollection<TagSessionSummary> TagSessionSummaries { get; set; }
    }
}
