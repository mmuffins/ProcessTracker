using MediatR;
using ProcessTrackerService.Core.Dto.Responses;

namespace ProcessTrackerService.Core.Dto.Requests
{
    public class TagToggleRequest : IRequest<GenericResponse>
    {
        public string Name { get; set; }
    }
}
