using MediatR;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Core.Specifications;
using System.Net;

namespace ProcessTrackerService.Core.Handlers
{
    public sealed class TagToggleHandler : IRequestHandler<TagToggleRequest, GenericResponse>
    {
        private readonly IAsyncRepository<Tag> _tagRepository;

        public TagToggleHandler(IAsyncRepository<Tag> tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<GenericResponse> Handle(TagToggleRequest request, CancellationToken cancellationToken)
        {
            // get tag from the DB and return error if there is no tag by this name
            var tag = await _tagRepository.FirstOrDefaultAsync(new GenericSpecification<Tag>(x => x.Name.ToLower().Equals(request.Name.ToLower())));
            if (tag == null)
                return new GenericResponse((int)HttpStatusCode.NotFound, false, "No tag found.");

            // set true if false and set false if true
            tag.Inactive = !tag.Inactive;
            await _tagRepository.UpdateAsync(tag);
            return new GenericResponse((int)HttpStatusCode.OK, true, "Tag has been " + (tag.Inactive ? "deactivated" : "activated") + " successfully.");
        }
    }

}
