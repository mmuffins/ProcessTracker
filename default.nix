{ lib
, fetchFromGitHub
, buildDotnetModule
, dotnetCorePackages
}:

# dotnet restore --packages nuget ./ProcessTracker.sln
# nuget-to-json nuget > deps.json
# rm -r nuget
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
    "ProcessTrackerService/ProcessTrackerService.csproj"
  ];

  nugetDeps = ./deps.json;
  executables = [ "processtracker" ];

  dotnet-sdk = dotnetCorePackages.sdk_9_0;
  dotnet-runtime = dotnetCorePackages.runtime_9_0;
  # buildPhase = ''
  #   tmp="$(mktemp -d)"
  #   dotnet publish ProcessTrackerService --configuration Relase --runtime linux-x64 --framework net8.0 --self-contained false -p:PublishSingleFile=true -p:PublishTrimmed=false -p:PublishReadyToRun=false --output publish
  # '';

  # installPhase = ''
  #   mkdir -p $out
  #   cp -r publish $out/bin
  # '';
}
