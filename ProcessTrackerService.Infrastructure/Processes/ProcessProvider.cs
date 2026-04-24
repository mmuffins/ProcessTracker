using System.Collections.Generic;
using System.Diagnostics;
using ProcessTrackerService.Core.Dto.Responses.ViewModels;
using ProcessTrackerService.Core.Interfaces;

namespace ProcessTrackerService.Infrastructure.Processes;

public sealed class ProcessProvider : IProcessProvider
{
    public IReadOnlyList<ProcessViewModel> GetProcesses()
    {
        var isLinux = OperatingSystem.IsLinux();
        var processes = Process.GetProcesses();
        var result = new List<ProcessViewModel>(processes.Length);

        foreach (var process in processes)
        {
            try
            {
                if (isLinux)
                {
                    // On Linux, we can get the command line from /proc, so we can include it in the view model.
                    result.Add(CreateLinuxProcessViewModel(process));
                    continue;
                }
                else
                {
                    // On Windows, getting the command line is more complex and may require additional permissions, so we skip it for now.
                    result.Add(CreateWindowsProcessViewModel(process));
                    continue;
                }
            }
            catch
            {
                // Processes might exit or deny access to module information between enumeration and inspection.
                // Skip those entries to match the production handler behavior, which swallows exceptions here.
            }
        }

        return result;
    }

    private ProcessViewModel CreateWindowsProcessViewModel(Process process)
    {
        var mainModule = process.MainModule;
        return new ProcessViewModel
        {
            Name = process.ProcessName,
            MainWindowTitle = process.MainWindowTitle,
            Description = mainModule?.FileVersionInfo.FileDescription,
            Path = mainModule?.FileName,
            CommandLine = null
        };
    }

    private ProcessViewModel CreateLinuxProcessViewModel(Process process)
    {
        var mainModule = process.MainModule;
        string? commandLine = null;
        if (ShouldGetCommandLine(process))
        {
            commandLine = GetCommandLine(process);
        }


        return new ProcessViewModel
        {
            Name = process.ProcessName,
            MainWindowTitle = process.MainWindowTitle,
            Description = mainModule?.FileVersionInfo.FileDescription,
            Path = mainModule?.FileName,
            CommandLine = commandLine
        };
    }

    private bool ShouldGetCommandLine(Process process)
    {
        switch (process.ProcessName)
        {
            case "steam.exe":
            case "python3":
            case "python":
            case "pv-adverb":
            case "reaper":
            case "umu.exe":
            case "Play Main Threa":
            case "wineserver":
            case "winedeevice.exe":
            case "lutris-wrapper":
            case "gamemoded":
            case "srt-bwrap":
                return true;
        }
        return false;
    }

    private string? GetCommandLine(Process process)
    {
        try
        {
            var bytes = File.ReadAllBytes($"/proc/{process.Id}/cmdline");
            return string.Join(' ', System.Text.Encoding.UTF8
                .GetString(bytes)
                .Split('\0', StringSplitOptions.RemoveEmptyEntries));
        }
        catch
        {
            return null;
        }
    }

}