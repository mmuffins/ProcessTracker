using MediatR;
using ProcessTrackerService.Core.Dto.Responses;

namespace ProcessTrackerService.Core.Dto.Requests
{
    public class DeleteTagRequest : IRequest<GenericResponse>
    {
        public string Name { get; set; }
    }
}
