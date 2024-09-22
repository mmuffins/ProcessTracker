namespace ProcessTrackerService.Core.Helpers
{
    public class AppSettings
    {
        public int HttpPort { get; set; }
        public int ProcessCheckDelay { get; set; }
        public int CushionDelay { get; set; }
        public string DateTimeFormat { get; set; }
        public string DateFormat { get; set; }
    }
}
