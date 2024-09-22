using MediatR;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Core.Specifications;
using System.Net;

namespace ProcessTrackerService.Core.Handlers
{
    public sealed class CreateFilterHandler : IRequestHandler<CreateFilterRequest, GenericResponse>
    {
        private readonly IAsyncRepository<Tag> _tagRepository;
        private readonly IAsyncRepository<Filter> _filterRepository;

        public CreateFilterHandler(IAsyncRepository<Tag> tagRepository, IAsyncRepository<Filter> filterRepository)
        {
            _tagRepository = tagRepository;
            _filterRepository = filterRepository;
        }

        public async Task<GenericResponse> Handle(CreateFilterRequest request, CancellationToken cancellationToken)
        {
            // get tag from the database and return error if tag not found
            var tag = await _tagRepository.FirstOrDefaultAsync(new GenericSpecification<Tag>(x => x.Name.ToLower().Equals(request.TagName.ToLower())));
            if (tag == null)
                return new GenericResponse((int)HttpStatusCode.NotFound, false, "Tag not found.");

            // add new filter against tag id fetched from the DB
            await _filterRepository.AddAsync(new Filter
            {
                TagId = tag.Id,
                FieldType = ((FieldTypeEnum)request.FieldType).ToString(),
                FilterType = ((FilterTypeEnum)request.FilterType).ToString(),
                FieldValue = request.Value,
                Inactive = false
            });
            return new GenericResponse((int)HttpStatusCode.OK, true, "Filter added successfully.");
        }
    }

}
