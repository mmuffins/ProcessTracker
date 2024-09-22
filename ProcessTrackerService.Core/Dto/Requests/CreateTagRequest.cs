using MediatR;
using ProcessTrackerService.Core.Dto.Responses;

namespace ProcessTrackerService.Core.Dto.Requests
{
    public class CreateTagRequest: IRequest<GenericResponse>
    {
        public string Name { get; set; }
    }
}
