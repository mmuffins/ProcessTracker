using MediatR;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Core.Specifications;
using System.Net;

namespace ProcessTrackerService.Core.Handlers
{
    public sealed class SessionRemoveHandler : IRequestHandler<SessionRemoveRequest, GenericResponse>
    {
        private readonly IAsyncRepository<TagSession> _tagSessionRepository;

        public SessionRemoveHandler(IAsyncRepository<TagSession> tagSessionRepository)
        {
            _tagSessionRepository = tagSessionRepository;
        }

        public async Task<GenericResponse> Handle(SessionRemoveRequest request, CancellationToken cancellationToken)
        {
            var beforeDate = DateTime.Today.AddDays(-1 * request.days);
            var sessions = await _tagSessionRepository.ListAsync(new GenericSpecification<TagSession>(x => x.CreationDate < beforeDate));
            if (sessions.Any())
                await _tagSessionRepository.DeleteBulkAsync(sessions.ToList());

            return new GenericResponse((int)HttpStatusCode.OK, true, "Success");
        }
    }

}
