using MediatR;
using Microsoft.Extensions.Configuration;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Helpers;
using ProcessTrackerService.Core.Interfaces.Repository;
using System.Globalization;
using System.Net;

namespace ProcessTrackerService.Core.Handlers
{
    public sealed class ReportHandler : IRequestHandler<ReportRequest, GenericResponse>
    {
        private readonly ITagSessionRepository _tagSessionRepository;
        private readonly IConfiguration _configuration;

        public ReportHandler(ITagSessionRepository tagSessionRepository, IConfiguration configuration)
        {
            _tagSessionRepository = tagSessionRepository;
            _configuration = configuration;
        }

        private AppSettings AppSettings
        {
            get
            {
                return _configuration.GetSection("AppSettings").Get<AppSettings>();
            }
        }
        public async Task<GenericResponse> Handle(ReportRequest request, CancellationToken cancellationToken)
        {
            DateTime startDate = default;
            DateTime endDate = default;
            // if start date is provided then convert it from string to DateTime datatype
            if (!string.IsNullOrEmpty(request.StartDate))
            {
                var isValidDate = DateTime.TryParseExact(request.StartDate, AppSettings.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate);
                if (!isValidDate)
                    return new GenericResponse((int)HttpStatusCode.ExpectationFailed, false, "Invalid start date format. Format must be " + AppSettings.DateFormat);
            }  
            // if end date is provided then convert it from string to DateTime datatype
            if (!string.IsNullOrEmpty(request.EndDate))
            {
                var isValidDate = DateTime.TryParseExact(request.EndDate, AppSettings.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate);
                if (!isValidDate)
                    return new GenericResponse((int)HttpStatusCode.ExpectationFailed, false, "Invalid end date format. Format must be " + AppSettings.DateFormat);
            }

            // Get report by the provided filters.
            var vmList = await _tagSessionRepository.GetTagsReport(request.TagName, !string.IsNullOrEmpty(request.StartDate) ? startDate : null, !string.IsNullOrEmpty(request.EndDate) ? endDate : null, AppSettings.DateFormat);

            return new GenericResponse(vmList, (int)HttpStatusCode.OK, true, "Success");
        }
    }

}
