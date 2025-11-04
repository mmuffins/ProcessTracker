using ProcessTrackerService.Core.Dto.Responses.ViewModels;

namespace ProcessTrackerService.Core.Interfaces;

public interface IProcessProvider
{
    IReadOnlyList<ProcessViewModel> GetProcesses();
}
