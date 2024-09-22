using MediatR;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Core.Specifications;
using System.Net;

namespace ProcessTrackerService.Core.Handlers
{
    public sealed class CreateTagHandler : IRequestHandler<CreateTagRequest, GenericResponse>
    {
        private readonly IAsyncRepository<Tag> _tagRepository;

        public CreateTagHandler(IAsyncRepository<Tag> tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<GenericResponse> Handle(CreateTagRequest request, CancellationToken cancellationToken)
        {
            // Duplicate check of the tag by name
            if (await _tagRepository.AnyAsync(new GenericSpecification<Tag>(x => x.Name.ToLower().Equals(request.Name.ToLower()))))
                return new GenericResponse((int)HttpStatusCode.ExpectationFailed, false, "Tag already exists.");

            // add tag in the database
            await _tagRepository.AddAsync(new Tag
            {
                Name = request.Name,
                Inactive = false
            });
            return new GenericResponse((int)HttpStatusCode.OK, true, "Tag added successfully.");
        }
    }

}
