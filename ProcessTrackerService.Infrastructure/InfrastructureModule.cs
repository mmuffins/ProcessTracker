using Microsoft.Extensions.DependencyInjection;
using ProcessTrackerService.Core.Interfaces.Repository;
using ProcessTrackerService.Infrastructure.Repository;

namespace ProcessTrackerService.Infrastructure
{
    public static class InfrastructureModule
    {
        public static void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<ITagSessionRepository, TagSessionRepository>();
        }
    }
}