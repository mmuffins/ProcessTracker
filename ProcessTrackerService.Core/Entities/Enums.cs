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
        Description = 3,
        MainWindowTitle = 4
    }
}
