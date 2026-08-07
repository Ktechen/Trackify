# Trackify CLI — build from the repo root as context:
#   docker build -f Source/Trackify.Cli/Dockerfile -t trackify-cli .
#
# BlueZ is always compiled in; the container runs on Linux, so AddLinuxLego selects the real BlueZ
# transport at runtime (OperatingSystem.IsLinux()). No build flags needed.
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
# NOTE: despite the file name this is a SQLite database (EF Core), not JSON — the name is kept so
# existing deployments keep finding their store. Don't "fix" it without renaming the file on the host.
ENV TRACKIFY_STORE=/data/trains.json

# Own /data as the app user BEFORE declaring the VOLUME: Docker seeds a fresh named/anonymous volume
# from the image directory, ownership included, so leaving it root-owned would make the store
# unwritable for a plain `docker run`. (A bind mount keeps the HOST's ownership instead — see below.)
RUN mkdir -p /data && chown $APP_UID:$APP_UID /data
VOLUME /data

# Run as the non-root `app` user the .NET base image provides (APP_UID=1654) — resolves docker:S6471.
# Two things on the HOST must line up, or BLE and the store fail (see Source/Trackify.Cli/ReadMe.md):
#   1. D-Bus: BlueZ grants org.bluez to root and to the `bluetooth` group only, so the container needs
#      that group — docker-compose.yml passes it via `group_add: ${BLUETOOTH_GID:-113}`.
#   2. Store: a BIND-mounted ./data keeps its HOST ownership (the chown above does not apply), so it
#      must be made writable by UID 1654 on the host: `sudo chown -R 1654:1654 data`.
# Escape hatch while verifying on a Pi: `user: "0:0"` in docker-compose.yml restores the old root run.
USER $APP_UID

ENTRYPOINT ["dotnet", "trackify.dll"]
CMD []
