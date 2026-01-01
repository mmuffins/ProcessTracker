# ProcessTracker Integration Tests

The integration tests exercise the production service registrations and data access layer against an in-memory SQLite database. The shared fixture in this project mirrors the backend configuration so that tests can interact with the same MediatR handlers, repositories, and Entity Framework Core context used by the worker service.

## Fixture utilities

The `IntegrationTestFixture` exposes helpers for common integration testing patterns:

- `ApplyConfigurationOverrides` can adjust configuration values before the host is built.
- `ResetDatabaseAsync` wipes and recreates the shared in-memory SQLite schema between tests.
- `ExecuteScopeAsync` / `ExecuteScopeAsync<TResult>` run arbitrary actions inside a scoped service provider.
- `ExecuteScopedServiceAsync` / `ExecuteScopedServiceAsync<TResult>` resolve a single service within a scope and pass it to your delegate.
- `CreateScope` / `CreateAsyncScope` and `GetRequiredService<T>` are available for manual scope control when needed.

## Requirements

- [.NET SDK 10.0](https://dotnet.microsoft.com/) or newer must be available on your PATH.
- The solution restores NuGet packages from the default feeds; ensure you have network connectivity for the first restore.

## Running the tests

To execute the full solution test suite (including these integration tests), run the following command from the repository root:

```bash
dotnet test ProcessTracker.sln
```

If you want to run only the integration tests, you can target this project directly:

```bash
dotnet test ProcessTracker.IntegrationTests/ProcessTracker.IntegrationTests.csproj
```

> **Note:** The fixture automatically provisions and resets an in-memory SQLite database for each test collection run. No manual database setup is required.
