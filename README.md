# Process Tracker
## Setup
### Windows
#### Manual installation
To manually install the application download the latest win-x64 zip file from the releases section and extract it to a folder. Move the appsettings.json to a valid location (see the configuration section), and ensure that the database path in the appsettings file exists.

The application can either be executed interactively. Alternatively the following command line can be used to create a service from the application:
```powershell
sc create "Process Tracker Service" binpath= "[Directory]\ProcessTrackerService.exe" start= auto
```
### Linux
#### Via rpm file
To install the application via rpm file, download the latest version from the releases section and install it with
```bash
rpm -ivh ProcessTracker-<version>.x86_64.rmp
```
#### NixOs flake
If you manage your NixOS system using flakes, you can install the application via the provided flake:
- Add the flake to your system configuration:
```
{
  description = "My NixOS Configuration";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    process-tracker = {
      url = "github:mmuffins/ProcessTracker";
      inputs.nixpkgs.follows = "nixpkgs";
    };
  };

  outputs = { self, nixpkgs, process-tracker, ... }:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs { inherit system; };
    in {
      nixosConfigurations.my-hostname = pkgs.lib.nixosSystem {
        system = "x86_64-linux";
        modules = [
          # Import the process tracker NixOS module from the flake.
          process-tracker.nixosModules.process-tracker

          # ... other modules
        ];
        configuration = { config, pkgs, ... }: {
          # Enable the process tracker service.
          services.process-tracker.enable = true;

          # Optionally, override additional options:
          # services.process-tracker.serviceConfig.Restart = "on-failure";
        };
      };
    };
}
- Rebuild your system
```
sudo nixos-rebuild switch --flake /path/to/your/flake#my-hostname
```

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
sc delete "Process Tracker Service"
```

## Configuration
### Config file location
The application is configured using a `appsettings.json` file. The application tries to locate the configuration file in different locations depending on the operating system. The search order is as follows:

#### Linux:
- The path configured in the `PROCESSTRACKER_APPSETTINGS_PATH` environment variable, if it is set and points to a valid file.
- `$XDG_CONFIG_HOME/processtracker/appsettings.json` if the `XDG_CONFIG_HOME` environment variable is set and points to a valid location.
- `/etc/processtracker/appsettings.json` if the application is running as the `root` user.
- `~/.config/processtracker/appsettings.json` as the default if no other option applies.

#### Windows:
- The path configured in the `PROCESSTRACKER_APPSETTINGS_PATH` environment variable, if it is set and points to a valid file.
- `C:\ProgramData\ProcessTracker\appsettings.json` as the default if the environment variable is not set or does not point to a valid file.


### Supported Properties
The following options are supported in the configuration file:
- `Logging` - Optional. Supports standard .net core logging settings, see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging
- `Appsettings`
  - `HttpPort`: TCP port to listen on for incoming connections. Defaults to 8001.
  - `DateTimeFormat`: Format for datetime strings throughout the application. Defaults to `yyyy-MM-dd HH:mm`.
  - `DateFormat`: Format for date strings throughout the application. Defaults to `yyyy-MM-dd`.
  - `ProcessCheckDelay`: Time in seconds between the state of running applications is checked. Defaults to 20.
  - `CushionDelay`: Time in seconds that has to elapse after a process cannot be found before it is considered to be terminated.  Defaults to 60.
  - `DatabasePath`: Path for the process tracker database defaults to `C:/ProgramData/ProcessTracker/processtracker.db` in windows and `/var/lib/processtracker/processtracker.db` in linux.

## Upgrading
### Via rpm file
To upgrade the application via rpm file, download the latest version from the releases section and upgrade it using
```bash
rpm -Uvh ProcessTracker-<version>.rmp
```

### NixOS
To updating the flake:
``` bash
nix flake update
```
Then rebuild your system:
```bash
sudo nixos-rebuild switch --flake /path/to/your/flake#my-hostname
```

# Database Migrations
To apply migrations and create a new database run the following from the root folder of the solution.
```powershell
dotnet ef migrations add InitialMigration --project ./ProcessTrackerService.Infrastructure --startup-project ./ProcessTrackerService
dotnet ef database update --project ./ProcessTrackerService.Infrastructure --startup-project ./ProcessTrackerService
```
