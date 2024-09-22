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
    public sealed class SessionAddHandler : IRequestHandler<SessionAddRequest, GenericResponse>
	{
		private readonly IAsyncRepository<TagSession> _tagSessionRepository;
		private readonly IAsyncRepository<Tag> _tagRepository;
        private readonly IConfiguration _configuration;

        public SessionAddHandler(IAsyncRepository<TagSession> tagSessionRepository, IAsyncRepository<Tag> tagRepository, IConfiguration configuration)
        {
            _tagSessionRepository = tagSessionRepository;
            _tagRepository = tagRepository;
            _configuration = configuration;
        }

        private AppSettings AppSettings
        {
            get
            {
                return _configuration.GetSection("AppSettings").Get<AppSettings>();
            }
        }

        public async Task<GenericResponse> Handle(SessionAddRequest request, CancellationToken cancellationToken)
		{
            DateTime startDate;
            DateTime endDate;

            // convert startdate and enddate from string to DateTime datatype
            var isValidDate = DateTime.TryParseExact(request.StartDate, AppSettings.DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate);
            if (!isValidDate)
                return new GenericResponse((int)HttpStatusCode.ExpectationFailed, false, "Invalid start date format. Format must be " + AppSettings.DateTimeFormat);

            isValidDate = DateTime.TryParseExact(request.EndDate, AppSettings.DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate);
            if (!isValidDate)
                return new GenericResponse((int)HttpStatusCode.ExpectationFailed, false, "Invalid end date format. Format must be " + AppSettings.DateTimeFormat);

            // get tag from the database and return error if no tag found
            var tag = await _tagRepository.FirstOrDefaultAsync(new GenericSpecification<Tag>(x => x.Name.ToLower().Equals(request.TagName.ToLower())));
            if (tag == null)
                return new GenericResponse((int)HttpStatusCode.NotFound, false, "No tag found.");

            await _tagSessionRepository.AddAsync(new TagSession
            {
                TagId = tag.Id,
                StartTime = startDate,
                EndTime = endDate,
                LastUpdateTime = endDate
            });

            return new GenericResponse((int)HttpStatusCode.OK, true, "Session added successfully.");
		}
	}

}
