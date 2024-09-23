using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ProcessTrackerService;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Infrastructure;
using ProcessTrackerService.Infrastructure.Data;
using ProcessTrackerService.Infrastructure.Repository;
using ProcessTrackerService.Server;
using System.IO;

var builder = Host.CreateApplicationBuilder(args);

var dbPath = builder.Configuration.GetConnectionString("ProcessDB") ?? "";
var dbFilePath = dbPath.Split('=')[1];

builder.Services.AddDbContext<PTServiceContext>(options =>
    options.UseSqlite(dbPath, b => b.MigrationsAssembly("ProcessTrackerService.Infrastructure")),
    ServiceLifetime.Transient);

// Create the DB if it doesn't exist when starting the application
if (!File.Exists(dbFilePath))
{
    Console.WriteLine("Database not found. Creating a new database...");
    using (var scope = builder.Services.BuildServiceProvider().CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<PTServiceContext>();
        context.Database.EnsureCreated();
    }
}

builder.Services.AddHostedService<Worker>();

builder.Services.AddSystemd();
builder.Services.AddWindowsService(option => option.ServiceName = "Process Tracker Service");

var coreassembly = AppDomain.CurrentDomain.Load("ProcessTrackerService.Core");
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(coreassembly));

builder.Services.AddSingleton<IHttpServer, HttpServer>();
builder.Services.AddScoped(typeof(IAsyncRepository<>), typeof(EfRepository<>));

builder.ConfigureContainer(new AutofacServiceProviderFactory(), builder =>
{
    // builder.RegisterModule(new CoreModule());
    builder.RegisterModule(new InfrastructureModule());
});

var host = builder.Build();
host.Run();