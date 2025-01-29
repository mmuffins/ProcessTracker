{ lib
, fetchFromGitHub
, buildDotnetModule
, dotnetCorePackages
}:

# dotnet restore --packages out ./ProcessTracker.sln
# nuget-to-json out > deps.json
# nix-build -E 'with import <nixpkgs> {}; callPackage ./default.nix {}'

buildDotnetModule rec {
  pname = "process-tracker";
  version = "1.0.461";

  meta = with lib; {
    description = "A cross-platform tool to track and report how long process were running";
    license = licenses.mit;
    platforms = platforms.linux;
  };

  src = ./.;

  projectFile = [
    "ProcessTrackerService.Core/ProcessTrackerService.Core.csproj"
    "ProcessTrackerService.Infrastructure/ProcessTrackerService.Infrastructure.csproj"
    "ProcessTrackerService/ProcessTrackerService.csproj"
  ];
  nugetDeps = ./deps.json;
  executables = [ "processtracker" ];

  dotnet-sdk = dotnetCorePackages.sdk_8_0;
  dotnet-runtime = dotnetCorePackages.runtime_8_0;
}