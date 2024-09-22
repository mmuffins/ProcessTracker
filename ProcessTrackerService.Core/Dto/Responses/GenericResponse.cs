using ProcessTrackerService.Core.Dto.Responses.ViewModels;
using ProcessTrackerService.Core.Interfaces;

namespace ProcessTrackerService.Core.Dto.Responses
{
    public class GenericResponse : APIResponseMessage
    {
        public readonly List<TagViewModel> Tags;
        public readonly List<TagsReportViewModel> Report;
        public readonly TagViewModel Tag;
        public readonly List<FilterViewModel> Filters;
        public readonly FilterViewModel Filter;
        public readonly string SettingValue;
        public GenericResponse(int statuscode, bool success = false, string message = null) : base(statuscode, success, message)
        {
        }
        public GenericResponse(List<TagViewModel> tags, int statuscode, bool success = false, string message = null) : base(statuscode, success, message)
        {
            Tags = tags;
        }
        public GenericResponse(TagViewModel tag, int statuscode, bool success = false, string message = null) : base(statuscode, success, message)
        {
            Tag = tag;
        }
        public GenericResponse(List<FilterViewModel> filters, int statuscode, bool success = false, string message = null) : base(statuscode, success, message)
        {
            Filters = filters;
        }
        public GenericResponse(FilterViewModel filter, int statuscode, bool success = false, string message = null) : base(statuscode, success, message)
        {
            Filter = filter;
        }
        public GenericResponse(List<TagsReportViewModel> report, int statuscode, bool success = false, string message = null) : base(statuscode, success, message)
        {
            Report = report;
        }
        public GenericResponse(string settingValue, int statuscode, bool success = false, string message = null) : base(statuscode, success, message)
        {
            SettingValue = settingValue;
        }
    }
}
