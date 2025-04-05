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
      appVersion = "1.0.911";
      dotnetVersion = "9_0";
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

            # Extra service options (if needed)
            # serviceConfig = lib.mkOption {
            #   type = lib.types.attrs;
            #   default = {};
            #   description = "Extra configuration options for the process tracker systemd unit.";
            # };
          };

          # Install the packages and create a systemd service
          config = lib.mkIf cfg.enable {
            home.packages = [
              cfg.package
              self.packages.${system}.process-tracker-cli
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
              };

              Install.WantedBy = [ "default.target" ];
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
