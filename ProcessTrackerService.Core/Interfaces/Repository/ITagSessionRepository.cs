using ProcessTrackerService.Core.Dto.Responses.ViewModels;
using ProcessTrackerService.Core.Entities;

namespace ProcessTrackerService.Core.Interfaces.Repository
{
    public interface ITagSessionRepository : IAsyncRepository<TagSession>
    {
        Task<List<TagSessionViewModel>> GetTagsWithSessions();
        Task<List<TagsReportViewModel>> GetTagsReport(string tagName, DateTime? startDate, DateTime? endDate, string format);
    }
}
