using ProcessTrackerService.Core.Interfaces;

namespace ProcessTrackerService.Core.Entities
{
    public class Filter : BaseEntity, IAggregateRoot
    {
        public int Id { get; set; }
        public string FilterType { get; set; }
        public string FieldType { get; set; }
        public string FieldValue { get; set; }
        public int TagId { get; set; }
        public bool Inactive { get; set; }
        public virtual Tag Tag { get; set; }
    }
}
