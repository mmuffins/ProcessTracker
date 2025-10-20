using System;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProcessTrackerService;
using ProcessTrackerService.Core.Helpers;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Infrastructure;
using ProcessTrackerService.Infrastructure.Data;
using ProcessTrackerService.Infrastructure.Repository;
using ProcessTrackerService.Server;

namespace ProcessTracker.IntegrationTests.Fixtures;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly HostApplicationBuilder _builder;
    private IHost? _host;
    private SqliteConnection? _connection;

    private static readonly IReadOnlyDictionary<string, string?> DefaultConfiguration = new Dictionary<string, string?>
    {
        ["Logging:LogLevel:Default"] = "Information",
        ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Information",
        ["AppSettings:HttpPort"] = "6200",
        ["AppSettings:ProcessCheckDelay"] = "5",
        ["AppSettings:CushionDelay"] = "5",
        ["AppSettings:DateTimeFormat"] = "yyyy-MM-dd HH:mm",
        ["AppSettings:DateFormat"] = "yyyy-MM-dd",
        ["AppSettings:DatabasePath"] = "integration-tests.db"
    };

    public IntegrationTestFixture()
    {
        _builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development
        });

        _builder.Configuration.AddInMemoryCollection(DefaultConfiguration);

        Configuration = _builder.Configuration;

        _builder.Services.AddSingleton<IConfiguration>(Configuration);
        _builder.Services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
        _builder.Services.AddOptions();
        _builder.Services.AddLogging();

        var coreAssembly = typeof(AppSettings).Assembly;
        _builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(coreAssembly));
        _builder.Services.AddScoped(typeof(IAsyncRepository<>), typeof(EfRepository<>));
        _builder.Services.AddSingleton<IHttpServer, HttpServer>();
        _builder.Services.AddHostedService<Worker>();

        InfrastructureModule.RegisterServices(_builder.Services);
    }

    public IConfiguration Configuration { get; }

    public IReadOnlyDictionary<string, string?> ConfigurationDefaults => DefaultConfiguration;

    public void ApplyConfigurationOverrides(IDictionary<string, string?> overrides)
    {
        ArgumentNullException.ThrowIfNull(overrides);

        foreach (var (key, value) in overrides)
        {
            Configuration[key] = value;
        }
    }

    public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Service provider has not been initialized.");

    public async Task InitializeAsync()
    {
        if (_host is not null)
        {
            return;
        }

        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        _builder.Services.AddSingleton<SqliteConnection>(_ => _connection!);
        _builder.Services.AddDbContext<PTServiceContext>((provider, options) =>
        {
            var connection = provider.GetRequiredService<SqliteConnection>();
            options.UseSqlite(connection, sql => sql.MigrationsAssembly(typeof(PTServiceContext).Assembly.FullName));
        }, ServiceLifetime.Transient);

        _host = _builder.Build();

        await EnsureDatabaseCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            if (_host is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                _host.Dispose();
            }

            _host = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async Task ResetDatabaseAsync()
    {
        await ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        });
    }

    public async Task SeedDatabaseAsync(Func<PTServiceContext, Task>? seeder = null)
    {
        await ResetDatabaseAsync();

        if (seeder is null)
        {
            return;
        }

        await ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            await seeder(context);
            await context.SaveChangesAsync();
        });
    }

    public async Task ExecuteScopeAsync(Func<IServiceProvider, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using var scope = Services.CreateAsyncScope();
        await action(scope.ServiceProvider);
    }

    public async Task<TResult> ExecuteScopeAsync<TResult>(Func<IServiceProvider, Task<TResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    public async Task ExecuteScopedServiceAsync<TService>(Func<TService, Task> action) where TService : notnull
    {
        ArgumentNullException.ThrowIfNull(action);

        await using var scope = Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        await action(service);
    }

    public async Task<TResult> ExecuteScopedServiceAsync<TService, TResult>(Func<TService, Task<TResult>> action) where TService : notnull
    {
        ArgumentNullException.ThrowIfNull(action);

        await using var scope = Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return await action(service);
    }

    public IServiceScope CreateScope() => Services.CreateScope();

    public AsyncServiceScope CreateAsyncScope() => Services.CreateAsyncScope();

    public TService GetRequiredService<TService>() where TService : notnull => Services.GetRequiredService<TService>();

    private async Task EnsureDatabaseCreatedAsync()
    {
        await ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            await context.Database.EnsureCreatedAsync();
        });
    }
}
