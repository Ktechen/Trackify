# Trackify

Configure and control **LEGO Powered Up** train hubs over **Bluetooth LE**, directly on-device — no
server/backend. BLE speaks the [LEGO Wireless Protocol (LWP) v3](https://lego.github.io/lego-ble-wireless-protocol-docs/)
via [SharpBrick.PoweredUp](https://github.com/sharpbrick/powered-up).

A Clean Architecture solution with **two front-ends over one shared core**: an [Uno Platform](https://platform.uno)
app (App / HMI / Web) and a [Spectre.Console](https://spectreconsole.net) CLI for a Raspberry Pi / Linux server.

## Screenshots

**Dashboard**
<img width="1003" alt="Dashboard" src="https://github.com/user-attachments/assets/ceb3b6d4-2356-4295-b155-7fffbbbddee1" />

**Controls**
<img width="1004" alt="Controls" src="https://github.com/user-attachments/assets/e16cce54-e177-42e6-9e38-367179f0eb7f" />

**Train editor**
<img width="1527" alt="Train editor" src="https://github.com/user-attachments/assets/d2651d75-3c3c-4e80-8543-13d7d5f427b2" />

## Architecture rules (must always hold)

Dependencies point **inward**; each layer only knows the ones to its left:

```
Domain  ←  Application  ←  Infrastructure  ←  Front-ends (HMI: CLI, Uno app = App/HMI/Web)
```

1. **Domain** (`Trackify.Domain`) — pure entities, enums, math. Depends on nothing (only the DI
   abstractions, contract-only). No UI, no logging, no EF, no BLE.
2. **Application** (`Trackify.Application`) — use cases + **ports** (interfaces like `ILegoService`,
   `ITrainRepository`). Depends only on Domain. UI- and transport-agnostic.
3. **Infrastructure** (`Trackify.Infrastructure`) — implements Application ports (EF Core + SQLite
   store, BlueZ hub transport). Depends on Application (+ Domain).
4. **Front-ends** (`Trackify.Cli`, `Trackify` Uno app) — depend on Application; reference
   Infrastructure **only at the composition root** to wire concrete implementations into DI.
5. **Never depend outward** (Domain must not reference Application, Application must not reference
   Infrastructure, …). Each layer owns its DI: `AddTrackifyDomain/Application/Infrastructure`.
6. **Enforced by the build**: project references only point inward, and namespace-matches-folder
   (`IDE0130`) + file-scoped namespaces (`IDE0161`) are errors (`Directory.Build.props`).

## Projects

| Project | Layer | Notes |
|---|---|---|
| `Source/Trackify.Domain` | Domain | Entities (`Train`, `TrackSegment`), enums, `SpeedFunction` |
| `Source/Trackify.Application` | Application | Ports, `TrainControlService`, `LegoinoCatalog`; hosts the mobile/WinRT `ILegoService` impls (multi-targeted per build host) |
| `Source/Trackify.Infrastructure` | Infrastructure | EF Core + SQLite store, BlueZ (Linux) transport |
| `Source/Trackify` | Front-end | Uno app — heads: android, ios, browserwasm (**Web**), desktop, windows |
| `Source/Trackify.Cli` | Front-end | Spectre.Console CLI for the Pi/Linux |
| `Test/Trackify.Tests` | Tests | xUnit, foldered by layer |

## Build & verify

```bash
# Shared core + CLI + tests
dotnet build Source/Trackify.Cli/Trackify.Cli.csproj
dotnet test  Test/Trackify.Tests/Trackify.Tests.csproj

# One Uno head (android / ios / browserwasm / desktop / windows)
dotnet build Source/Trackify/Trackify.csproj -f net10.0-desktop
```

SDK: `global.json` pins `9.0.100` with `rollForward: latestMajor` — the newest installed major is
used (net10 heads require the .NET 10 SDK); CI provisions .NET 8, 9 and 10.

## The train store (`trackify.db`)

Trains are persisted in a **SQLite** database via **EF Core** (`SqliteTrainRepository`, repository-style
`ITrainRepository`; the schema is created automatically). Default location:
`~/.config/Trackify/trackify.db` (Linux) / `%APPDATA%\Trackify\trackify.db` (Windows), overridable
with the `TRACKIFY_STORE` environment variable. The Uno app and the CLI share the same schema.

## CLI

```bash
trackify                       # dashboard (banner, saved trains, commands)
trackify discover              # scan for hubs
trackify list                  # saved trains
trackify drive "Blauer Zug" --speed 40 --color Green   # run until Ctrl+C
trackify stop  "Blauer Zug"
trackify color "Blauer Zug" Blue
```

See [Source/Trackify.Cli/ReadMe.md](Source/Trackify.Cli/ReadMe.md) for deployment (Raspberry Pi,
Docker, systemd autostart).

## CI/CD (GitHub Actions)

| Workflow | Trigger | Does |
|---|---|---|
| `ci.yml` | PR / push to `master` | Build the CLI + shared core and run tests (pre-merge gate) |
| `android-apk.yml` | tag `v*` / manual | Build the Android APK |
| `cli-arm64.yml` | tag `v*` / manual | Publish the self-contained `linux-arm64` CLI for the Pi |

---

# Trackify (Deutsch)

Konfiguriert und steuert **LEGO Powered Up** Zug-Hubs über **Bluetooth LE**, direkt auf dem Gerät —
kein Server/Backend. BLE spricht das [LEGO Wireless Protocol (LWP) v3](https://lego.github.io/lego-ble-wireless-protocol-docs/)
über [SharpBrick.PoweredUp](https://github.com/sharpbrick/powered-up).

Eine Clean-Architecture-Solution mit **zwei Front-Ends über einem gemeinsamen Kern**: eine
[Uno-Platform](https://platform.uno)-App (App / HMI / Web) und eine
[Spectre.Console](https://spectreconsole.net)-CLI für Raspberry Pi / Linux-Server.
Screenshots siehe oben.

## Architektur-Regeln (gelten immer)

Abhängigkeiten zeigen **nach innen**; jede Schicht kennt nur die links von ihr:

```
Domain  ←  Application  ←  Infrastructure  ←  Front-Ends (HMI: CLI, Uno-App = App/HMI/Web)
```

1. **Domain** (`Trackify.Domain`) — reine Entities, Enums, Mathematik. Hängt von nichts ab (nur den
   DI-Abstraktionen, reiner Vertrag). Kein UI, kein Logging, kein EF, kein BLE.
2. **Application** (`Trackify.Application`) — Use Cases + **Ports** (Interfaces wie `ILegoService`,
   `ITrainRepository`). Hängt nur von Domain ab. UI- und transport-neutral.
3. **Infrastructure** (`Trackify.Infrastructure`) — implementiert die Application-Ports (EF-Core-+-
   SQLite-Store, BlueZ-Hub-Transport). Hängt von Application (+ Domain) ab.
4. **Front-Ends** (`Trackify.Cli`, `Trackify`-Uno-App) — hängen von Application ab; referenzieren
   Infrastructure **nur im Composition Root**, um konkrete Implementierungen ins DI zu hängen.
5. **Nie nach außen abhängen** (Domain darf Application nicht kennen, Application nicht Infrastructure
   …). Jede Schicht besitzt ihr DI: `AddTrackifyDomain/Application/Infrastructure`.
6. **Vom Build erzwungen**: Projektverweise zeigen nur nach innen, und Namespace-passt-zu-Ordner
   (`IDE0130`) + file-scoped Namespaces (`IDE0161`) sind Fehler (`Directory.Build.props`).

## Projekte

| Projekt | Schicht | Hinweise |
|---|---|---|
| `Source/Trackify.Domain` | Domain | Entities (`Train`, `TrackSegment`), Enums, `SpeedFunction` |
| `Source/Trackify.Application` | Application | Ports, `TrainControlService`, `LegoinoCatalog`; enthält die Mobile-/WinRT-`ILegoService`-Impls (multi-targeted je nach Build-Host) |
| `Source/Trackify.Infrastructure` | Infrastructure | EF-Core-+-SQLite-Store, BlueZ-Transport (Linux) |
| `Source/Trackify` | Front-End | Uno-App — Heads: android, ios, browserwasm (**Web**), desktop, windows |
| `Source/Trackify.Cli` | Front-End | Spectre.Console-CLI für Pi/Linux |
| `Test/Trackify.Tests` | Tests | xUnit, nach Schicht in Ordner sortiert |

## Bauen & verifizieren

```bash
# Gemeinsamer Kern + CLI + Tests
dotnet build Source/Trackify.Cli/Trackify.Cli.csproj
dotnet test  Test/Trackify.Tests/Trackify.Tests.csproj

# Ein Uno-Head (android / ios / browserwasm / desktop / windows)
dotnet build Source/Trackify/Trackify.csproj -f net10.0-desktop
```

SDK: `global.json` pinnt `9.0.100` mit `rollForward: latestMajor` — das höchste installierte Major
wird genutzt (net10-Heads brauchen das .NET-10-SDK); die CI stellt .NET 8, 9 und 10 bereit.

## Der Train-Store (`trackify.db`)

Züge werden in einer **SQLite**-Datenbank über **EF Core** persistiert (`SqliteTrainRepository`,
Repository-artiges `ITrainRepository`; das Schema wird automatisch angelegt). Standardpfad:
`~/.config/Trackify/trackify.db` (Linux) / `%APPDATA%\Trackify\trackify.db` (Windows), überschreibbar
per Umgebungsvariable `TRACKIFY_STORE`. Uno-App und CLI teilen dasselbe Schema.

## CLI

```bash
trackify                       # Dashboard (Banner, gespeicherte Züge, Befehle)
trackify discover              # nach Hubs scannen
trackify list                  # gespeicherte Züge
trackify drive "Blauer Zug" --speed 40 --color Green   # fahren bis Ctrl+C
trackify stop  "Blauer Zug"
trackify color "Blauer Zug" Blue
```

Deployment (Raspberry Pi, Docker, systemd-Autostart) siehe [Source/Trackify.Cli/ReadMe.md](Source/Trackify.Cli/ReadMe.md).

## CI/CD (GitHub Actions)

| Workflow | Auslöser | Zweck |
|---|---|---|
| `ci.yml` | PR / Push auf `master` | CLI + gemeinsamen Kern bauen und Tests laufen lassen (Pre-Merge-Gate) |
| `android-apk.yml` | Tag `v*` / manuell | Android-APK bauen |
| `cli-arm64.yml` | Tag `v*` / manuell | Self-contained `linux-arm64`-CLI für den Pi veröffentlichen |
