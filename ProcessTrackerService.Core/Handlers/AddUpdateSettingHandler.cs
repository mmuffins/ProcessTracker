using MediatR;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Core.Specifications;
using System.Net;

namespace ProcessTrackerService.Core.Handlers
{

    public sealed class AddUpdateSettingHandler : IRequestHandler<AddUpdateSettingRequest, GenericResponse>
    {
        private readonly IAsyncRepository<Setting> _settingRepository;

        public AddUpdateSettingHandler(IAsyncRepository<Setting> settingRepository)
        {
            _settingRepository = settingRepository;
        }

        public async Task<GenericResponse> Handle(AddUpdateSettingRequest request, CancellationToken cancellationToken)
        {
            // get setting from the DB
            var setting = await _settingRepository.FirstOrDefaultAsync(new GenericSpecification<Setting>(x => x.SettingName.Equals(request.Setting.ToString())));
            if (setting == null)
                return new GenericResponse((int)HttpStatusCode.NotFound, false, "Setting not found.");

            // change value and update it in the DB
            setting.Value = request.value;
            await _settingRepository.UpdateAsync(setting);

            return new GenericResponse((int)HttpStatusCode.OK, true, "Success");
        }
    }

}
