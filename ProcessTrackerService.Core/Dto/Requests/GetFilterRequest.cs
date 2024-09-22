using MediatR;
using ProcessTrackerService.Core.Dto.Responses;

namespace ProcessTrackerService.Core.Dto.Requests
{
    public class GetFilterRequest : IRequest<GenericResponse>
    {
        public string TagName { get; set; }
        public int FilterID { get; set; }
        public bool? inactive { get; set; }
    }
}
