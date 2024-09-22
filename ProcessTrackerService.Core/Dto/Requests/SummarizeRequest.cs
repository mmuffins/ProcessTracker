using MediatR;
using ProcessTrackerService.Core.Dto.Responses;

namespace ProcessTrackerService.Core.Dto.Requests
{
    public class SummarizeRequest : IRequest<GenericResponse>
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}
