# Recreate Database
To create a new database run the following from the root folder of the solution.
```powershell
dotnet ef migrations add InitialMigration --project ./ProcessTrackerService.Infrastructure --startup-project ./ProcessTrackerService
dotnet ef database update --project ./ProcessTrackerService.Infrastructure --startup-project ./ProcessTrackerService
```

# Configuration
## Config file
The application is configured using a `appsettings.json` file. The application tries to locate the configuration file in different locations depending on the operating system. The search order is as follows:

### Linux:
- The path configured in the `PROCESSTRACKER_APPSETTINGS_PATH` environment variable, if it is set and points to a valid file.
- `$XDG_CONFIG_HOME/processtracker/appsettings.json` if the `XDG_CONFIG_HOME` environment variable is set and points to a valid location.
- `/etc/processtracker/appsettings.json` if the application is running as the `root` user.
- `~/.config/processtracker/appsettings.json` as the default if no other option applies.

### Windows:
- The path configured in the `PROCESSTRACKER_APPSETTINGS_PATH` environment variable, if it is set and points to a valid file.
- `C:\ProgramData\ProcessTracker\appsettings.json` as the default if the environment variable is not set or does not point to a valid file.