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
      appVersion = "";
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
          # dotnet restore --packages nuget-restore ./ProcessTracker.sln
          # nuget-to-json nuget-restore > deps.json
          # rm -r nuget-restore
          nugetDeps = ./deps.json;
          executables = [ "processtracker" ];
        };
      };

      defaultPackage."${system}" = self.packages."${system}".process-tracker;

      # Optional: Define an app for easier invocation via “nix run”.
      # apps.process-tracker = {
      #   type = "app";
      #   # Adjust the path to the built executable if necessary.
      #   program = "${self.packages.\"x86_64-linux\".process-tracker}/bin/processtracker";
      # };
    };
}
