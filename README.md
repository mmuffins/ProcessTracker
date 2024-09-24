# Process Tracker
## Setup
### Windows
#### Manual installation
To manually install the application download the latest win-x64 zip file from the releases section and extract it to a folder. Move the appsettings.json to a valid location (see the configuration section), and ensure that the database path in the appsettings file exists.

The application can either be executed interactively. Alternatively the following command line can be used to create a service from the application:
```powershell
sc create "Process Tracker" binpath= "[Directory]\ProcessTrackerService.exe" start= auto
```

## Deletion
### Windows
To completele remove the application, delete the previously created items:
- The application
- The appsettings.json file
- The database file
- The application service

The following command can be used to delete the service:
```powershell
sc delete "Process Tracker"
```

## Configuration
### Config file
The application is configured using a `appsettings.json` file. The application tries to locate the configuration file in different locations depending on the operating system. The search order is as follows:

#### Linux:
- The path configured in the `PROCESSTRACKER_APPSETTINGS_PATH` environment variable, if it is set and points to a valid file.
- `$XDG_CONFIG_HOME/processtracker/appsettings.json` if the `XDG_CONFIG_HOME` environment variable is set and points to a valid location.
- `/etc/processtracker/appsettings.json` if the application is running as the `root` user.
- `~/.config/processtracker/appsettings.json` as the default if no other option applies.

#### Windows:
- The path configured in the `PROCESSTRACKER_APPSETTINGS_PATH` environment variable, if it is set and points to a valid file.
- `C:\ProgramData\ProcessTracker\appsettings.json` as the default if the environment variable is not set or does not point to a valid file.

# Database Migrations
To apply migrations and create a new database run the following from the root folder of the solution.
```powershell
dotnet ef migrations add InitialMigration --project ./ProcessTrackerService.Infrastructure --startup-project ./ProcessTrackerService
dotnet ef database update --project ./ProcessTrackerService.Infrastructure --startup-project ./ProcessTrackerService
```
