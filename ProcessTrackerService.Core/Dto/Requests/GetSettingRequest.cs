using MediatR;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Entities;

namespace ProcessTrackerService.Core.Dto.Requests
{
    public class GetSettingRequest : IRequest<GenericResponse>
    {
        public SettingEnum Setting { get; set; }
    }
}
