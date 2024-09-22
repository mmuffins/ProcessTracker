using MediatR;
using ProcessTrackerService.Core.Dto.Responses;

namespace ProcessTrackerService.Core.Dto.Requests
{
    public class DeleteFilterRequest : IRequest<GenericResponse>
    {
        public int FilterID { get; set; }
    }
}
