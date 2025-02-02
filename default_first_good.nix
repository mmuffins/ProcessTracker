{ lib
, fetchFromGitHub
, buildDotnetModule
, dotnetCorePackages
}:
let
# dotnet restore --packages nuget ./ProcessTracker.sln
# nuget-to-json nuget > deps.json
# rm -r nuget
# nix-build -E 'with import <nixpkgs> {}; callPackage ./default.nix {}'

# removed:
# Microsoft.AspNetCore.App.Runtime.linux-x64
# Microsoft.AspNetCore.App.Runtime.win-x64
# Microsoft.NETCore.App.Runtime.linux-x64
# Microsoft.NETCore.App.Runtime.win-x64

dotnet-sdk = dotnetCorePackages.sdk_9_0;
dotnet-runtime = dotnetCorePackages.runtime_9_0;
nugetDeps = ./deps.json;
configuration = "Release";
runtime = "linux-x64";
TargetFramework = "net9.0";
version = "1.0.461";


core = buildDotnetModule {
  inherit nugetDeps dotnet-sdk dotnet-runtime configuration runtime TargetFramework version;


  pname = "ProcessTrackerService.Core";
  src = ./.;

  projectFile = [
    "ProcessTrackerService.Core/ProcessTrackerService.Core.csproj"
  ];

  preInstall = ''
    cp ProcessTrackerService.Core/bin/${configuration}/${TargetFramework}/${runtime}/* ProcessTrackerService.Core/bin/${configuration}/${TargetFramework}
  '';

  packNupkg = true;
};

infrastructure = buildDotnetModule {
  inherit nugetDeps dotnet-sdk dotnet-runtime configuration runtime TargetFramework version;

  pname = "ProcessTrackerService.Infrastructure";
  src = ./.;

  projectFile = [
    "ProcessTrackerService.Infrastructure/ProcessTrackerService.Infrastructure.csproj"
    ];

  preInstall = ''
    cp ProcessTrackerService.Infrastructure/bin/${configuration}/${TargetFramework}/${runtime}/* ProcessTrackerService.Infrastructure/bin/${configuration}/${TargetFramework}
  '';

  projectReferences = [ core ];

  packNupkg = true;
};

application = buildDotnetModule {
  inherit nugetDeps dotnet-sdk dotnet-runtime configuration runtime TargetFramework version;

  pname = "process-tracker";

  meta = with lib; {
    description = "A cross-platform tool to track and report how long process were running";
    license = licenses.mit;
    platforms = platforms.linux;
  };

  src = ./.;

  # projectFile = [
  #   "ProcessTrackerService.Core/ProcessTrackerService.Core.csproj"
  #   "ProcessTrackerService.Infrastructure/ProcessTrackerService.Infrastructure.csproj"
  #   "ProcessTrackerService/ProcessTrackerService.csproj"
  # ];

  projectFile = [
    # "ProcessTrackerService.Core/ProcessTrackerService.Core.csproj"
    # "ProcessTrackerService.Infrastructure/ProcessTrackerService.Infrastructure.csproj"
    "ProcessTrackerService/ProcessTrackerService.csproj"
  ];
  projectReferences = [ core infrastructure ];


  executables = [ "processtracker" ];

  # buildPhase = ''
  #   tmp="$(mktemp -d)"
  #   dotnet publish ProcessTrackerService --configuration ${configuration} --runtime ${runtime} --framework ${TargetFramework} --self-contained false -p:PublishSingleFile=true -p:PublishTrimmed=false -p:PublishReadyToRun=false --output publish
  # '';

  # installPhase = ''
  #   mkdir -p $out
  #   cp -r publish $out/bin
  # '';
};

in application