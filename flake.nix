{
  description = "process-tracker";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    process-tracker-cli = {
      url = "github:mmuffins/ProcessTrackerCLI";
      inputs.nixpkgs.follows = "nixpkgs";
    };
  };

  outputs =
    {
      self,
      nixpkgs,
      process-tracker-cli,
      ...
    }@inputs:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs { inherit system; };
      appVersion = "1.0.1971";
      dotnetVersion = "10_0";
      makeProcessTrackerUpdateScript =
        port:
        pkgs.writeShellScriptBin "process-tracker-update" ''
          set -eu

          if [ "$#" -ne 2 ]; then
            echo "usage: process-tracker-update <start-date> <end-date>" >&2
            exit 1
          fi

          start_date="$1"
          end_date="$2"

          json_body=$(printf '{"StartDate": "%s", "EndDate": "%s"}' "$start_date" "$end_date")
          response=$(${pkgs.curl}/bin/curl -s -X POST -H "Content-Type: application/json" -d "$json_body" "http://localhost:${toString port}/api/Summarize")
          success=$(echo "$response" | ${pkgs.jq}/bin/jq -r '.Success')
          statuscode=$(echo "$response" | ${pkgs.jq}/bin/jq -r '.StatusCode')
          if [ "$success" != "true" ]; then
            echo "process-tracker-update failed: success=$success status=$statuscode" >&2
            exit 1
          fi
        '';
    in
    {
      inherit system;

      packages."${system}" = {
        process-tracker = pkgs.buildDotnetModule {
          pname = "process-tracker";
          version = "${appVersion}";

          meta = with pkgs.lib; {
            description = "A cross-platform tool to track and report running processes";
            license = licenses.mit;
            platforms = [ system ];
          };

          dotnet-sdk = pkgs.dotnetCorePackages."sdk_${dotnetVersion}";
          dotnet-runtime = pkgs.dotnetCorePackages."runtime_${dotnetVersion}";

          src = self;

          projectFile = [
            "ProcessTrackerService/ProcessTrackerService.csproj"
          ];

          # to manually update dependencies:
          # dotnet restore --use-current-runtime --packages nuget-restore ./ProcessTracker.sln
          # nuget-to-json nuget-restore > deps.json
          # rm -r nuget-restore
          # When building locally, you may need to manually remove the packages
          # - microsoft.netcore.app.runtime.linux-x64
          # - microsoft.aspnetcore.app.runtime.linux-x64
          # from the created deps.json, not sure why
          nugetDeps = ./deps.json;
          executables = [ "processtracker" ];
        };
        process-tracker-cli = inputs.process-tracker-cli.packages.${system}.process-tracker-cli;
        process-tracker-update = makeProcessTrackerUpdateScript 8001;
      };

      defaultPackage."${system}" = self.packages."${system}".process-tracker;

      nixosModules.process-tracker =
        {
          config,
          lib,
          pkgs,
          ...
        }:
        let
          cfg = config.services.process-tracker;
          processTrackerUpdateScript = makeProcessTrackerUpdateScript cfg.port;

          generatedSettingsFile = pkgs.writeText "process-tracker.json" (
            builtins.toJSON {
              Logging = {
                LogLevel = {
                  Default = cfg.logging.logLevel;
                  Microsoft.EntityFrameworkCore.Database.Command = cfg.logging.database;
                  Microsoft.Hosting.Lifetime = cfg.logging.logLevel;
                };
              };

              AppSettings = {
                HttpPort = cfg.port;
                DateTimeFormat = cfg.dateTimeFormat;
                DateFormat = cfg.dateFormat;
                ProcessCheckDelay = cfg.processCheckDelay;
                CushionDelay = cfg.cushionDelay;
                DatabasePath = cfg.databasePath;
              };
            }
          );

          effectiveSettingsFile =
            if cfg.settingsFile != null then cfg.settingsFile else generatedSettingsFile;
        in
        {
          options.services.process-tracker = {

            enable = lib.mkEnableOption "Enable the process tracker service";

            package = lib.mkOption {
              type = lib.types.package;
              default = self.packages.${system}.process-tracker;
              description = "The package to run as the process tracker service.";
            };

            notifyOnFailure = lib.mkOption {
              type = lib.types.bool;
              default = true;
              description = "Enable notifications when the service fails. Requires libnotify to be installed.";
            };

            settingsFilePath = lib.mkOption {
              type = lib.types.path;
              readOnly = true;
              default = effectiveSettingsFile;
              description = "Path to the process-tracker appsettings JSON file in the Nix store.";
            };

            settingsFile = lib.mkOption {
              type = lib.types.nullOr lib.types.path;
              default = null;
              description = ''
                Full path to the main appsettings JSON file.

                If null, the module generates a non-secret appsettings.json automatically
                from the other module options.
              '';
            };

            logging = {
              logLevel = lib.mkOption {
                type = lib.types.str;
                default = "Information";
                description = "Default logging level written into the generated appsettings.json.";
              };

              database = lib.mkOption {
                type = lib.types.str;
                default = "Warning";
                description = "Database logging level written into the generated appsettings.json.";
              };
            };

            port = lib.mkOption {
              type = lib.types.int;
              default = 8001;
              description = "HTTP port for the process tracker API.";
            };

            dateTimeFormat = lib.mkOption {
              type = lib.types.str;
              default = "yyyy-MM-dd HH:mm";
              description = "Date and time format used in the API responses.";
            };

            dateFormat = lib.mkOption {
              type = lib.types.str;
              default = "yyyy-MM-dd";
              description = "Date format used in the API responses.";
            };

            processCheckDelay = lib.mkOption {
              type = lib.types.int;
              default = 20;
              description = "Delay in seconds between process checks.";
            };

            cushionDelay = lib.mkOption {
              type = lib.types.int;
              default = 60;
              description = "Additional delay in seconds added to process check delay to account for timing issues.";
            };

            databasePath = lib.mkOption {
              type = lib.types.str;
              default = "${config.xdg.dataHome}/processtracker/processtracker.db";
              description = "File path for the process tracker SQLite database.";
            };

          };

          # Install the packages and create a systemd service
          config = lib.mkIf cfg.enable {
            home.packages = [
              cfg.package
              self.packages.${system}.process-tracker-cli
              processTrackerUpdateScript
            ];

            systemd.user.services.process-tracker = {
              Unit = {
                Description = "Process Tracker Service";
                After = [ "graphical-session.target" ];
                OnFailure = lib.mkIf cfg.notifyOnFailure [ "process-tracker-notify.service" ];
              };

              Service = {
                ExecStart = "${lib.getExe' cfg.package "processtracker"}";
                Restart = "on-failure";
                Environment = [
                  "PROCESSTRACKER_APPSETTINGS_PATH=${effectiveSettingsFile}"
                ];
              };

              Install.WantedBy = [ "graphical-session.target" ];
            };

            systemd.user.services.process-tracker-notify = lib.mkIf cfg.notifyOnFailure {
              Unit = {
                Description = "Notify user if Process Tracker service fails";
                After = [ "graphical-session.target" ];
              };

              Service = {
                Type = "oneshot";
                ExecStart = "${pkgs.bash}/bin/bash -c '${pkgs.libnotify}/bin/notify-send --urgency critical --app-name process-tracker --icon dialog-error \"Process Tracker service failed. See systemctl --user status process-tracker\"'";
              };
            };
          };
        };

      process-tracker-cli = process-tracker-cli.nixosModules.process-tracker-cli;
    };
}
