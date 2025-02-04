{
  description = "process-tracker";
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";

  outputs = { 
    self, 
    nixpkgs, 
    ...  
  } @ inputs: let
      system = "x86_64-linux";
      pkgs = import nixpkgs {
        inherit system;
      };
      appVersion = "1.0.691";
      dotnetVersion = "9_0";
    in {
      inherit system;

      packages."${system}" = {
        process-tracker = pkgs.buildDotnetModule rec {
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
          nugetDeps = ./deps.json;
          executables = [ "processtracker" ];
        };
      };

      defaultPackage."${system}" = self.packages."${system}".process-tracker;

      nixosModules.process-tracker = { config, lib, pkgs, ... }:
        let
          cfg = config.services.process-tracker;
        in {
          options.services.process-tracker = {
            enable = lib.mkEnableOption "Enable the process tracker service";
            package = lib.mkOption {
              type = lib.types.package;
              default = self.packages.${system}.process-tracker;
              description = "The package to run as the process tracker service.";
            };
            # Extra service options (if needed)
            # serviceConfig = lib.mkOption {
            #   type = lib.types.attrs;
            #   default = {};
            #   description = "Extra configuration options for the process tracker systemd unit.";
            # };
          };

          # Install the package and create a systemd service
          config = lib.mkIf cfg.enable {
            # Also make it available to run interactively
            home.packages = [ cfg.package ];

            systemd.user.services.process-tracker = {
              Unit = {
                Description = "Process Tracker Service";
                After = [ "graphical-session.target" ];
              };

              Service = {
                ExecStart = "${lib.getExe' cfg.package "process-tracker"}";
                Restart = "on-failure";
              };

              Install.WantedBy = [ "default.target" ];
            };
          };
        };
  };
}
