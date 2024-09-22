using MediatR;
using ProcessTrackerService.Core.Dto.Responses;

namespace ProcessTrackerService.Core.Dto.Requests
{
    public class GetTagsRequest : IRequest<GenericResponse>
    {
        public string Name { get; set; }
        public bool? Inactive { get; set; }
    }
}
