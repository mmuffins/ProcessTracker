using MediatR;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Entities;

namespace ProcessTrackerService.Core.Dto.Requests
{
    public class AddUpdateSettingRequest : IRequest<GenericResponse>
    {
        public SettingEnum Setting { get; set; }
        public string value { get; set; }
    }
}
