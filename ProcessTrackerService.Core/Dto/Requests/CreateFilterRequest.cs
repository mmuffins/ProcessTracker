using MediatR;
using ProcessTrackerService.Core.Dto.Responses;

namespace ProcessTrackerService.Core.Dto.Requests
{
    public class CreateFilterRequest : IRequest<GenericResponse>
    {
        public string TagName { get; set; }
        public int FieldType { get; set; }
        public int FilterType { get; set; }
        public string Value { get; set; }
    }
}
