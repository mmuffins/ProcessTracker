using MediatR;
using ProcessTrackerService.Core.Dto.Responses;

namespace ProcessTrackerService.Core.Dto.Requests
{
    public class SessionRemoveRequest : IRequest<GenericResponse>
    {
        public int days { get; set; }
    }
}
