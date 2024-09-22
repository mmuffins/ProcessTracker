using MediatR;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Core.Specifications;
using System.Net;

namespace ProcessTrackerService.Core.Handlers
{
    public sealed class GetSettingHandler : IRequestHandler<GetSettingRequest, GenericResponse>
    {
        private readonly IAsyncRepository<Setting> _settingRepository;

        public GetSettingHandler(IAsyncRepository<Setting> settingRepository)
        {
            _settingRepository = settingRepository;
        }

        public async Task<GenericResponse> Handle(GetSettingRequest request, CancellationToken cancellationToken)
        {
            // Get setting from the DB
            var setting = await _settingRepository.FirstOrDefaultAsync(new GenericSpecification<Setting>(x => x.SettingName.Equals(request.Setting.ToString())));
            // if no setting found and setting name is tracking paused then add this setting into the DB with default value of false (tracking upaused)
            if (setting == null && request.Setting == SettingEnum.TrackingPaused)
            {
                await _settingRepository.AddAsync(new Setting
                {
                    SettingName = request.Setting.ToString(),
                    Value = false.ToString()
                });
                return new GenericResponse(false.ToString(), (int)HttpStatusCode.OK, true, "Success");
            }
            else
                return new GenericResponse(setting?.Value, (int)HttpStatusCode.OK, true, "Success");
        }
    }

}
