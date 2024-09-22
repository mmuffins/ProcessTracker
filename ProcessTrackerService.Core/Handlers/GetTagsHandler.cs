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
    public sealed class GetTagsHandler : IRequestHandler<GetTagsRequest, GenericResponse>
    {
        private readonly IAsyncRepository<Tag> _tagRepository;

        public GetTagsHandler(IAsyncRepository<Tag> tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<GenericResponse> Handle(GetTagsRequest request, CancellationToken cancellationToken)
        {
            // get single tag by name from DB
            if (!string.IsNullOrEmpty(request.Name))
            {
                var tag = await _tagRepository.FirstOrDefaultAsync(new GenericSpecification<Tag>(x => x.Name.ToLower().Equals(request.Name.ToLower())));
                return tag != null ? new GenericResponse(new TagViewModel
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    Inactive = tag.Inactive
                }, (int)HttpStatusCode.OK, true, "Success") : new GenericResponse((int)HttpStatusCode.NotFound, false, "No tag found.");
            }
            // get all the tags in the DB
            else
            {
                List<TagViewModel> vmList = new List<TagViewModel>();
                var tags = await _tagRepository.ListAsync(new GenericSpecification<Tag>(x => request.Inactive == null || x.Inactive == request.Inactive));
                if (tags != null && tags.Any())
                    vmList = tags.Select(x => new TagViewModel
                    {
                        Id = x.Id,
                        Inactive = x.Inactive,
                        Name = x.Name
                    }).ToList();
                return new GenericResponse(vmList, (int)HttpStatusCode.OK, true, "Success");
            }
        }
    }

}
