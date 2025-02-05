{
  description = "process-tracker";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    process-tracker-cli.url = "github:mmuffins/ProcessTrackerCLI";
  };

  outputs = { 
    self,
    nixpkgs,
    process-tracker-cli,
    ...  
  } @ inputs: let
      system = "x86_64-linux";
      pkgs = import nixpkgs { inherit system; };
      appVersion = "1.0.731";
      dotnetVersion = "9_0";
    in {
      inherit system;

      packages."${system}" = rec {
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
          # When building locally, you may need to manually remove the packages
          # - microsoft.netcore.app.runtime.linux-x64
          # - microsoft.aspnetcore.app.runtime.linux-x64
          # from the created deps.json, not sure why
          nugetDeps = ./deps.json;
          executables = [ "processtracker" ];
        };
          process-tracker-cli = process-tracker-cli.packages.${system}.process-tracker;
      };

      defaultPackage."${system}" = self.packages."${system}".process-tracker;

      nixosModules.process-tracker = { config, lib, pkgs, ... }:
        let
          cfg = config.services.process-tracker;
          cli = self.packages.${system}.process-tracker-cli;
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

          # Install the packages and create a systemd service
          config = lib.mkIf cfg.enable {
            home.packages = [ 
              cfg.package
              cli.package
            ];

            systemd.user.services.process-tracker = {
              Unit = {
                Description = "Process Tracker Service";
                After = [ "graphical-session.target" ];
              };

              Service = {
                ExecStart = "${lib.getExe' cfg.package "processtracker"}";
                Restart = "on-failure";
              };

              Install.WantedBy = [ "default.target" ];
            };
          };
        };
        
        process-tracker-cli = process-tracker-cli.nixosModules.process-tracker-cli;
  };
}
