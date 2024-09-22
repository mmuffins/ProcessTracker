using MediatR;
using ProcessTrackerService.Core.Dto.Responses;

namespace ProcessTrackerService.Core.Dto.Requests
{
    public class EnableDisableTagRequest : IRequest<GenericResponse>
    {
        public string name { get; set; }
    }
}
