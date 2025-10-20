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
        private const string InvalidFormatMessage = "Invalid {0} format. Format must be {1}";
        private const string MissingFormatMessage = "Date/time format configuration is missing.";

        private readonly IAsyncRepository<TagSession> _tagSessionRepository;
        private readonly IAsyncRepository<Tag> _tagRepository;
        private readonly IConfiguration _configuration;

        public SessionAddHandler(IAsyncRepository<TagSession> tagSessionRepository, IAsyncRepository<Tag> tagRepository, IConfiguration configuration)
        {
            _tagSessionRepository = tagSessionRepository;
            _tagRepository = tagRepository;
            _configuration = configuration;
        }

        private AppSettings AppSettings => _configuration.GetSection("AppSettings").Get<AppSettings>();

        public async Task<GenericResponse> Handle(SessionAddRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.TagName))
            {
                return new GenericResponse((int)HttpStatusCode.BadRequest, false, "Tag name is required.");
            }

            var tagName = request.TagName.Trim();
            if (tagName.Length == 0)
            {
                return new GenericResponse((int)HttpStatusCode.BadRequest, false, "Tag name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.StartDate) || string.IsNullOrWhiteSpace(request.EndDate))
            {
                return new GenericResponse((int)HttpStatusCode.BadRequest, false, "Start and end dates are required.");
            }

            var dateTimeFormat = AppSettings?.DateTimeFormat;
            if (string.IsNullOrWhiteSpace(dateTimeFormat))
            {
                return new GenericResponse((int)HttpStatusCode.InternalServerError, false, MissingFormatMessage);
            }

            if (!DateTime.TryParseExact(request.StartDate, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
            {
                return new GenericResponse((int)HttpStatusCode.ExpectationFailed, false, string.Format(CultureInfo.InvariantCulture, InvalidFormatMessage, "start date", dateTimeFormat));
            }

            if (!DateTime.TryParseExact(request.EndDate, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
            {
                return new GenericResponse((int)HttpStatusCode.ExpectationFailed, false, string.Format(CultureInfo.InvariantCulture, InvalidFormatMessage, "end date", dateTimeFormat));
            }

            if (startDate >= endDate)
            {
                return new GenericResponse((int)HttpStatusCode.BadRequest, false, "Start date must be earlier than end date.");
            }

            var lowerCaseName = tagName.ToLowerInvariant();
            var tag = await _tagRepository.FirstOrDefaultAsync(
                new GenericSpecification<Tag>(x => x.Name != null && x.Name.ToLower() == lowerCaseName));
            if (tag == null)
            {
                return new GenericResponse((int)HttpStatusCode.NotFound, false, "No tag found.");
            }

            var overlaps = await _tagSessionRepository.AnyAsync(
                new GenericSpecification<TagSession>(
                    x => x.TagId == tag.Id
                        && x.StartTime < endDate
                        && (x.EndTime ?? x.LastUpdateTime) > startDate));

            if (overlaps)
            {
                return new GenericResponse((int)HttpStatusCode.Conflict, false, "Session overlaps with an existing entry.");
            }

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
