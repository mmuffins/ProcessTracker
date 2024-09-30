using Microsoft.EntityFrameworkCore;
using ProcessTrackerService;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Infrastructure;
using ProcessTrackerService.Infrastructure.Data;
using ProcessTrackerService.Infrastructure.Repository;
using ProcessTrackerService.Server;
using System.Runtime.InteropServices;

var builder = Host.CreateApplicationBuilder(args);

var configFilePath = GetConfigFilePath();

builder.Configuration
    .AddJsonFile(configFilePath, optional: false, reloadOnChange: true);

var dbPath = builder.Configuration.GetSection("AppSettings:DatabasePath").Value ?? "";

// Ensure the folder structure for the database exists
var dbDirectory = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
{
    Directory.CreateDirectory(dbDirectory);
}

builder.Services.AddDbContext<PTServiceContext>(options =>
    options.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly("ProcessTrackerService.Infrastructure")),
    ServiceLifetime.Transient);

// Create the DB if it doesn't exist when starting the application
if (!File.Exists(dbPath))
{
    builder.Logging.AddConsole();
    var serviceProvider = builder.Services.BuildServiceProvider();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("Database not found. Creating a new database...");
    try
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PTServiceContext>();
            context.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "An error occurred while creating the database.");
        throw;
    }
}

builder.Services.AddHostedService<Worker>();

if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    builder.Services.AddSystemd();
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Services.AddWindowsService(option => option.ServiceName = "Process Tracker Service");
}

var coreAssembly = AppDomain.CurrentDomain.Load("ProcessTrackerService.Core");
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(coreAssembly));

builder.Services.AddSingleton<IHttpServer, HttpServer>();
builder.Services.AddScoped(typeof(IAsyncRepository<>), typeof(EfRepository<>));

// Call method to configure infrastructure services
InfrastructureModule.RegisterServices(builder.Services);

var host = builder.Build();
host.Run();

string GetConfigFilePath()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        return GetConfigFilePathLinux();
    }

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
