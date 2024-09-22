namespace ProcessTrackerService.Core.Dto.Responses.ViewModels
{
    public class FilterViewModel
    {
        public int Id { get; set; }
        public string Filter { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public bool Disabled { get; set; }
    }
}
