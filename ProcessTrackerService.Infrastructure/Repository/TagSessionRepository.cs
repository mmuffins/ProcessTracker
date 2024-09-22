using Microsoft.EntityFrameworkCore;
using ProcessTrackerService.Core.Dto.Responses.ViewModels;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Interfaces.Repository;
using ProcessTrackerService.Infrastructure.Data;

namespace ProcessTrackerService.Infrastructure.Repository
{
    public class TagSessionRepository : EfRepository<TagSession>, ITagSessionRepository
    {
        public TagSessionRepository(PTServiceContext dbContext) : base(dbContext)
        {
        }

        public async Task<List<TagSessionViewModel>> GetTagsWithSessions()
        {
            // linq to sql query to get all the tags aloing with their latest session if there is any
            var tags = await (from tag in _dbContext.Tags
                              from session in _dbContext.TagSessions
                              .Where(x => x.TagId == tag.Id && x.EndTime == null)
                              .OrderByDescending(x => x.SessionId)
                              .Take(1)
                              .DefaultIfEmpty()
                              select new TagSessionViewModel
                              {
                                  TagId = tag.Id,
                                  SessionId = session != null ? session.SessionId : null
                              }).ToListAsync();

            return tags;
        }

        public async Task<List<TagsReportViewModel>> GetTagsReport(string tagName, DateTime? startDate, DateTime? endDate, string format)
        {
            // Get session from the database and perform filters
            var summary = _dbContext.TagSessionSummary.AsQueryable();
            if (!string.IsNullOrEmpty(tagName))
                summary = summary.Where(x => x.Tag.Name.ToLower().Equals(tagName.ToLower()));
            if (startDate != null)
                summary = summary.Where(x => x.Day >= startDate);
            if (endDate != null)
                summary = summary.Where(x => x.Day <= endDate.Value.AddHours(23).AddMinutes(59).AddSeconds(59));

            summary = summary.Include(x => x.Tag);

            List<TagsReportViewModel> vmList = new List<TagsReportViewModel>();
            foreach (var tagSession in summary.GroupBy(x => x.TagId))
            {
                // calculate session times of the tag by summing up total minutes each tag ran
                TagsReportViewModel vm = new TagsReportViewModel();
                vm.Name = tagSession.First().Tag.Name;

                double totalMinutes = Math.Round(tagSession.Sum(x => x.Seconds) / 60);
                var tagSessionOrdered = tagSession.OrderBy(x => x.Day);
                var time = TimeSpan.FromMinutes(totalMinutes);
                vm.TotalActiveTime = string.Format("{0}:{1:00}", (int)time.TotalHours, time.Minutes);
                vm.FirstOccurence = tagSessionOrdered.First().Day.ToString(format);

                var lastSession = tagSession.OrderByDescending(x => x.Day).First();
                vm.LastOccurence = lastSession.Day.ToString(format);

                vmList.Add(vm);
            }
            return vmList;
        }
    }
}
