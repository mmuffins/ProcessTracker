using Microsoft.Extensions.DependencyInjection;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Core.Interfaces.Repository;
using ProcessTrackerService.Infrastructure.Processes;
using ProcessTrackerService.Infrastructure.Repository;
using ProcessTrackerService.Infrastructure.Time;

namespace ProcessTrackerService.Infrastructure;

public static class InfrastructureModule
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ITagSessionRepository, TagSessionRepository>();
        services.AddSingleton<ProcessProvider>();
        services.AddSingleton<IProcessProvider>(provider => provider.GetRequiredService<ProcessProvider>());
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
    }
}
