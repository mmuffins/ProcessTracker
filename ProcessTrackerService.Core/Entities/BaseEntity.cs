namespace ProcessTrackerService.Core.Entities
{
    public class BaseEntity
    {
        public DateTime? CreationDate { get; set; } = DateTime.Now;
    }
}
