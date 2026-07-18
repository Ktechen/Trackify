# Trackify CLI — build from the repo root as context:
#   docker build -f Source/Trackify.Cli/Dockerfile -t trackify-cli .
#
# Building INSIDE a Linux container means the LINUX compile flag turns on automatically
# (TrackifyLinux auto-detects the Linux build host), so the real BlueZ transport is compiled in.
# The image is multi-arch by base image: on a Raspberry Pi, docker pulls the arm64 variants.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first with only the project/props files, so source edits don't bust the restore cache.
COPY global.json Directory.Build.props Directory.Build.targets Directory.Packages.props ./
COPY Source/Trackify.Domain/Trackify.Domain.csproj Source/Trackify.Domain/
COPY Source/Trackify.Application/Trackify.Application.csproj Source/Trackify.Application/
COPY Source/Trackify.Infrastructure/Trackify.Infrastructure.csproj Source/Trackify.Infrastructure/
COPY Source/Trackify.Cli/Trackify.Cli.csproj Source/Trackify.Cli/
RUN dotnet restore Source/Trackify.Cli/Trackify.Cli.csproj

COPY Source/Trackify.Domain/ Source/Trackify.Domain/
COPY Source/Trackify.Application/ Source/Trackify.Application/
COPY Source/Trackify.Infrastructure/ Source/Trackify.Infrastructure/
COPY Source/Trackify.Cli/ Source/Trackify.Cli/
RUN dotnet publish Source/Trackify.Cli/Trackify.Cli.csproj -f net10.0 -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .

# The train store lives on a mounted volume so it survives container rebuilds.
ENV TRACKIFY_STORE=/data/trains.json
VOLUME /data

ENTRYPOINT ["dotnet", "trackify.dll"]
CMD []
