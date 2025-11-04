using System.Collections.Generic;
using System.Diagnostics;
using ProcessTrackerService.Core.Dto.Responses.ViewModels;
using ProcessTrackerService.Core.Interfaces;

namespace ProcessTrackerService.Infrastructure.Processes;

public sealed class ProcessProvider : IProcessProvider
{
    public IReadOnlyList<ProcessViewModel> GetProcesses()
    {
        var processes = Process.GetProcesses();
        var result = new List<ProcessViewModel>(processes.Length);

        foreach (var process in processes)
        {
            try
            {
                var mainModule = process.MainModule;
                result.Add(new ProcessViewModel
                {
                    Name = process.ProcessName,
                    MainWindowTitle = process.MainWindowTitle,
                    Description = mainModule?.FileVersionInfo.FileDescription,
                    Path = mainModule?.FileName
                });
            }
            catch
            {
                // Processes might exit or deny access to module information between enumeration and inspection.
                // Skip those entries to match the production handler behavior, which swallows exceptions here.
            }
        }

        return result;
    }
}
