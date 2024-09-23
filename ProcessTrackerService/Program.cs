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
using System.Runtime.InteropServices;

var builder = Host.CreateApplicationBuilder(args);

var configFilePath = GetConfigFilePath();

builder.Configuration
    .AddJsonFile(configFilePath, optional: false, reloadOnChange: true);

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
    builder.RegisterModule(new InfrastructureModule());
});

var host = builder.Build();
host.Run();


string GetConfigFilePath()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        return GetConfigFilePathLinux();
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        return GetConfigFilePathWindows();
    }

    throw new PlatformNotSupportedException("Unsupported platform.");
}

string GetConfigFilePathLinux()
{
    // Priority 1: Environment variable
    var configPathEnv = Environment.GetEnvironmentVariable("PROCESSTRACKER_APPSETTINGS_PATH");
    if (!string.IsNullOrEmpty(configPathEnv) && File.Exists(configPathEnv))
    {
        return configPathEnv;
    }

    // Priority 2: XDG_CONFIG_HOME
    var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
    if (!string.IsNullOrEmpty(xdgConfigHome))
    {
        var xdgConfigPath = Path.Combine(xdgConfigHome, "processtracker", "appsettings.json");
        if (File.Exists(xdgConfigPath))
        {
            return xdgConfigPath;
        }
    }

    // Priority 3: Check if running as root
    if (Environment.UserName == "root")
    {
        var etcConfigPath = "/etc/processtracker/appsettings.json";
        if (File.Exists(etcConfigPath))
        {
            return etcConfigPath;
        }
    }

    // Priority 4: Default to user's home directory .config path
    var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    var userConfigPath = Path.Combine(homeDir, ".config", "processtracker", "appsettings.json");
    return userConfigPath;
}

string GetConfigFilePathWindows()
{
    // Priority 1: Environment variable
    var configPathEnv = Environment.GetEnvironmentVariable("PROCESSTRACKER_APPSETTINGS_PATH");
    if (!string.IsNullOrEmpty(configPathEnv) && File.Exists(configPathEnv))
    {
        return configPathEnv;
    }

    // Priority 2: Default path in ProgramData on system drive
    var systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
    var programDataPath = Path.Combine(systemDrive, "ProgramData", "ProcessTracker", "appsettings.json");

    return programDataPath;
}