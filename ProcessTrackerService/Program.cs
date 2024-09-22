using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ProcessTrackerService;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Infrastructure;
using ProcessTrackerService.Infrastructure.Data;
using ProcessTrackerService.Infrastructure.Repository;
using ProcessTrackerService.Server;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<PTServiceContext>(options => options.UseSqlite(connectionString: builder.Configuration.GetConnectionString("ProcessDB") ?? "", b => b.MigrationsAssembly("ProcessTrackerService.Infrastructure")), ServiceLifetime.Transient);

builder.Services.AddHostedService<Worker>();

builder.Services.AddSystemd();
builder.Services.AddWindowsService(option => option.ServiceName = "Process Tracker Service");

var coreassembly = AppDomain.CurrentDomain.Load("ProcessTrackerService.Core");
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(coreassembly));

builder.Services.AddSingleton<IHttpServer, HttpServer>();
builder.Services.AddScoped(typeof(IAsyncRepository<>), typeof(EfRepository<>));

builder.ConfigureContainer(new AutofacServiceProviderFactory(), builder =>
{
    //builder.RegisterModule(new CoreModule());
    builder.RegisterModule(new InfrastructureModule());
});

var host = builder.Build();
host.Run();
