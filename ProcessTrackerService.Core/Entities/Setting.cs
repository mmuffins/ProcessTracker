using ProcessTrackerService.Core.Interfaces;

namespace ProcessTrackerService.Core.Entities
{
    public class Setting : BaseEntity, IAggregateRoot
    {
        public string SettingName { get; set; }
        public string Value { get; set; }
    }
}
