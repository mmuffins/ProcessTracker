using MediatR;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Core.Specifications;
using System.Net;

namespace ProcessTrackerService.Core.Handlers
{
    public sealed class DeleteFilterHandler : IRequestHandler<DeleteFilterRequest, GenericResponse>
    {
        private readonly IAsyncRepository<Filter> _filterRepository;

        public DeleteFilterHandler(IAsyncRepository<Filter> filterRepository)
        {
            _filterRepository = filterRepository;
        }
        public async Task<GenericResponse> Handle(DeleteFilterRequest request, CancellationToken cancellationToken)
        {
            // get filter by id from the DB
            var filter = await _filterRepository.FirstOrDefaultAsync(new GenericSpecification<Filter>(x => x.Id == request.FilterID));
            if (filter == null)
                return new GenericResponse((int)HttpStatusCode.NotFound, false, "No filter found.");

            // delete filter from the DB
            await _filterRepository.DeleteAsync(filter);
            return new GenericResponse((int)HttpStatusCode.OK, true, "Filter Deleted Successfully.");
        }
    }

}
