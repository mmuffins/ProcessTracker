namespace ProcessTrackerService.Core.Entities
{
    public enum SettingEnum
    {
        TrackingPaused
    }
    public enum FilterTypeEnum
    {
        StartWith = 1,
        EndWith = 2,
        Contain = 3,
        Equal = 4
    }

    public enum FieldTypeEnum
    {
        Name = 1,
        Path = 2,
        CommandLine = 3,
        Description = 4,
        MainWindowTitle = 5,
    }
}
