using MediatR;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Dto.Responses.ViewModels;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Core.Specifications;
using System.Net;

namespace ProcessTrackerService.Core.Handlers
{
    public sealed class GetFilterHandler : IRequestHandler<GetFilterRequest, GenericResponse>
    {
        private readonly IAsyncRepository<Filter> _filterRepository;

        public GetFilterHandler(IAsyncRepository<Filter> filterRepository)
        {
            _filterRepository = filterRepository;
        }

        public async Task<GenericResponse> Handle(GetFilterRequest request, CancellationToken cancellationToken)
        {
            // Get single filter by ID
            if (request.FilterID > 0)
            {
                var filter = await _filterRepository.FirstOrDefaultAsync(new GenericSpecification<Filter>(x => x.Id == request.FilterID));
                return filter != null ? new GenericResponse(new FilterViewModel
                {
                    Id = filter.Id,
                    Filter = filter.FieldType,
                    Type = filter.FilterType,
                    Value = filter.FieldValue,
                    Disabled = filter.Inactive
                }, (int)HttpStatusCode.OK, true, "Success") : new GenericResponse((int)HttpStatusCode.NotFound, false, "No filter found.");
            }
            // Get all the filters from DB
            else
            {
                List<FilterViewModel> vmList = new List<FilterViewModel>();
                var filters = await _filterRepository.ListAsync(new GenericSpecification<Filter>(x => x.Tag.Name.ToLower().Equals(request.TagName.ToLower()) && (request.inactive == null || x.Inactive == request.inactive)));
                if (filters != null && filters.Any())
                    vmList = filters.Select(x => new FilterViewModel
                    {
                        Id = x.Id,
                        Filter = x.FieldType,
                        Type = x.FilterType,
                        Value = x.FieldValue,
                        Disabled = x.Inactive
                    }).ToList();
                return new GenericResponse(vmList, (int)HttpStatusCode.OK, true, "Success");
            }
        }
    }

}
