using System;
using System.Collections.Generic;
using System.Linq;
using ProcessTrackerService.Core.Dto.Responses.ViewModels;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Infrastructure.Processes;

namespace ProcessTracker.IntegrationTests.Fixtures;

public sealed class SwitchableProcessProvider : IProcessProvider
{
    private readonly ProcessProvider _systemProvider;
    private readonly object _syncRoot = new();
    private IReadOnlyList<ProcessViewModel>? _processes;
    private bool _useSystem = true;

    public SwitchableProcessProvider(ProcessProvider systemProvider)
    {
        _systemProvider = systemProvider;
    }

    public IReadOnlyList<ProcessViewModel> GetProcesses()
    {
        lock (_syncRoot)
        {
            if (_useSystem)
            {
                return _systemProvider.GetProcesses();
            }

            return _processes ?? Array.Empty<ProcessViewModel>();
        }
    }

    public void UseSystemProcesses()
    {
        lock (_syncRoot)
        {
            _useSystem = true;
            _processes = null;
        }
    }

    public void SetProcesses(IEnumerable<ProcessViewModel> processes)
    {
        lock (_syncRoot)
        {
            _processes = processes?.Select(CloneProcess).ToList() ?? new List<ProcessViewModel>();
            _useSystem = false;
        }
    }

    public void ClearProcesses()
    {
        lock (_syncRoot)
        {
            _processes = Array.Empty<ProcessViewModel>();
            _useSystem = false;
        }
    }

    private static ProcessViewModel CloneProcess(ProcessViewModel process)
    {
        return new ProcessViewModel
        {
            Name = process.Name,
            Description = process.Description,
            MainWindowTitle = process.MainWindowTitle,
            Path = process.Path
        };
    }
}
