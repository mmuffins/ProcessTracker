using MediatR;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Core.Specifications;
using System.Net;

namespace ProcessTrackerService.Core.Handlers
{
    public sealed class DeleteTagHandler : IRequestHandler<DeleteTagRequest, GenericResponse>
    {
        private readonly IAsyncRepository<Tag> _tagRepository;

        public DeleteTagHandler(IAsyncRepository<Tag> tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<GenericResponse> Handle(DeleteTagRequest request, CancellationToken cancellationToken)
        {
            // get tag from the database and return error if no tag found
            var tag = await _tagRepository.FirstOrDefaultAsync(new GenericSpecification<Tag>(x => x.Name.ToLower().Equals(request.Name.ToLower())));
            if (tag == null)
                return new GenericResponse((int)HttpStatusCode.NotFound, false, "No tag found.");

            // delete tag in the database
            await _tagRepository.DeleteAsync(tag);
            return new GenericResponse((int)HttpStatusCode.OK, true, "Tag Deleted Successfully.");
        }
    }

}
