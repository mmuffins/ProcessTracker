using MediatR;
using ProcessTrackerService.Core.Dto.Responses;

namespace ProcessTrackerService.Core.Dto.Requests
{
    public class ReportRequest : IRequest<GenericResponse>
    {
        public string TagName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}
