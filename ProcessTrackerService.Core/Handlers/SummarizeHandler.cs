using MediatR;
using Microsoft.Extensions.Configuration;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Helpers;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Core.Specifications;
using System.Globalization;
using System.Net;

namespace ProcessTrackerService.Core.Handlers
{
    public sealed class SummarizeHandler : IRequestHandler<SummarizeRequest, GenericResponse>
    {
        private readonly IAsyncRepository<TagSession> _tagSessionRepository;
        private readonly IAsyncRepository<TagSessionSummary> _tagSessionSummaryRepository;
        private readonly IConfiguration _configuration;

        public SummarizeHandler(IAsyncRepository<TagSession> tagSessionRepository, IAsyncRepository<TagSessionSummary> tagSessionSummaryRepository, IConfiguration configuration)
        {
            _tagSessionRepository = tagSessionRepository;
            _tagSessionSummaryRepository = tagSessionSummaryRepository;
            _configuration = configuration;
        }

        private AppSettings AppSettings
        {
            get
            {
                return _configuration.GetSection("AppSettings").Get<AppSettings>();
            }
        }
        public async Task<GenericResponse> Handle(SummarizeRequest request, CancellationToken cancellationToken)
        {
            DateTime startDate;
            DateTime endDate;

            // convert startdate and enddate from string to DateTime datatype
            var isValidDate = DateTime.TryParseExact(request.StartDate, AppSettings.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate);
            if (!isValidDate)
                return new GenericResponse((int)HttpStatusCode.ExpectationFailed, false, "Invalid start date format. Format must be " + AppSettings.DateFormat);

            isValidDate = DateTime.TryParseExact(request.EndDate, AppSettings.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate);
            if (!isValidDate)
                return new GenericResponse((int)HttpStatusCode.ExpectationFailed, false, "Invalid end date format. Format must be " + AppSettings.DateFormat);

            // extract session within provided date range
            var sessions = await _tagSessionRepository.ListAsync(new GenericSpecification<TagSession>(x => !x.Tag.Inactive && x.EndTime != null && x.StartTime >= startDate && x.StartTime <= endDate.AddHours(23).AddMinutes(59).AddSeconds(59)));

            // extract session summaries within provided date range
            var sessionSummary = (await _tagSessionSummaryRepository.ListAsync(new GenericSpecification<TagSessionSummary>(x => !x.Tag.Inactive && x.Day >= startDate && x.Day <= endDate.AddHours(23).AddMinutes(59).AddSeconds(59)))).ToList();


            List<TagSessionSummary> toAdd = new List<TagSessionSummary>();
            List<TagSessionSummary> toUpdate = new List<TagSessionSummary>();

            // group sessions by tag and day
            foreach (var session in sessions.GroupBy(x => new { x.TagId, x.StartTime.Date }))
            {
                var summary = sessionSummary.FirstOrDefault(x => x.TagId == session.Key.TagId && x.Day == session.Key.Date);
                if (summary != null)
                {
                    toUpdate.Add(new TagSessionSummary
                    {
                        SummaryId = summary.SummaryId,
                        TagId = summary.TagId,
                        CreationDate = summary.CreationDate,
                        Day = summary.Day,
                        Seconds = session.Sum(x => (x.EndTime.Value - x.StartTime).TotalSeconds)
                    });

                    // do this so we can delete data from the summary that does not exit in session table but exit in summary table within specified time frame
                    sessionSummary.Remove(summary);
                }
                else
                {
                    toAdd.Add(new TagSessionSummary
                    {
                        TagId = session.Key.TagId,
                        Day = session.Key.Date,
                        Seconds = session.Sum(x => (x.EndTime.Value - x.StartTime).TotalSeconds)
                    });
                }
            }

            // bulk database operation to add, update and delete in summary table
            if (toAdd.Any())
                await _tagSessionSummaryRepository.AddBulkAsync(toAdd);

            if (toUpdate.Any())
                await _tagSessionSummaryRepository.UpdateBulkAsync(toUpdate);

            if (sessionSummary != null && sessionSummary.Any())
                await _tagSessionSummaryRepository.DeleteBulkAsync(sessionSummary);

            return new GenericResponse((int)HttpStatusCode.OK, true, "Success");
        }
    }

}
