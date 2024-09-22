using MediatR;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Core.Specifications;
using System.Net;

namespace ProcessTrackerService.Core.Handlers
{
    public sealed class FilterToggleHandler : IRequestHandler<FilterToggleRequest, GenericResponse>
    {
        private readonly IAsyncRepository<Filter> _filterRepository;

        public FilterToggleHandler(IAsyncRepository<Filter> filterRepository)
        {
            _filterRepository = filterRepository;
        }

        public async Task<GenericResponse> Handle(FilterToggleRequest request, CancellationToken cancellationToken)
        {
            // check if filter exist in the DB
            var filter = await _filterRepository.FirstOrDefaultAsync(new GenericSpecification<Filter>(x => x.Id == request.FilterID));
            if (filter == null)
                return new GenericResponse((int)HttpStatusCode.NotFound, false, "No filter found.");

            // set true if false and set false if true
            filter.Inactive = !filter.Inactive;
            await _filterRepository.UpdateAsync(filter);
            return new GenericResponse((int)HttpStatusCode.OK, true, "Filter has been " + (filter.Inactive ? "deactivated" : "activated") + " successfully.");
        }
    }

}
