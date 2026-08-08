# Trackify

## Status

| Category | Badge |
| --- | --- |
| CI | [![CI](https://github.com/Ktechen/Trackify/actions/workflows/ci.yml/badge.svg)](https://github.com/Ktechen/Trackify/actions/workflows/ci.yml) |
| CodeQL | [![CodeQL](https://github.com/Ktechen/Trackify/actions/workflows/codeql.yml/badge.svg)](https://github.com/Ktechen/Trackify/security/code-scanning) |
| Android APK | [![Android APK](https://github.com/Ktechen/Trackify/actions/workflows/android-apk.yml/badge.svg)](https://github.com/Ktechen/Trackify/actions/workflows/android-apk.yml) |
| CLI linux-arm64 | [![CLI linux-arm64](https://github.com/Ktechen/Trackify/actions/workflows/cli-arm64.yml/badge.svg)](https://github.com/Ktechen/Trackify/actions/workflows/cli-arm64.yml) |
| License | [![License](https://img.shields.io/github/license/Ktechen/Trackify)](LICENSE) |
| Framework | [![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com) |
| Quality Gate | [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Ktechen_Trackify&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Ktechen_Trackify) |
| Bugs | [![Bugs](https://sonarcloud.io/api/project_badges/measure?project=Ktechen_Trackify&metric=bugs)](https://sonarcloud.io/summary/new_code?id=Ktechen_Trackify) |
| Vulnerabilities | [![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=Ktechen_Trackify&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=Ktechen_Trackify) |
| Code Smells | [![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=Ktechen_Trackify&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=Ktechen_Trackify) |
| Security Hotspots | [![Security Hotspots](https://sonarcloud.io/api/project_badges/measure?project=Ktechen_Trackify&metric=security_hotspots)](https://sonarcloud.io/summary/new_code?id=Ktechen_Trackify) |
| Security Rating | [![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=Ktechen_Trackify&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=Ktechen_Trackify) |
| Reliability Rating | [![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=Ktechen_Trackify&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=Ktechen_Trackify) |
| Maintainability Rating | [![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=Ktechen_Trackify&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=Ktechen_Trackify) |
| Technical Debt | [![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=Ktechen_Trackify&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=Ktechen_Trackify) |
| Duplicated Lines | [![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=Ktechen_Trackify&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=Ktechen_Trackify) |
| Coverage | [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Ktechen_Trackify&metric=coverage)](https://sonarcloud.io/summary/new_code?id=Ktechen_Trackify) |

A Clean Architecture solution with **two front-ends over one shared core**: an [Uno Platform](https://platform.uno)
app (App / HMI / Web) and a [Spectre.Console](https://spectreconsole.net) CLI for a Raspberry Pi / Linux server.

## Screenshots

### Dashboard
<img width="1003" alt="Dashboard" src="https://github.com/user-attachments/assets/ceb3b6d4-2356-4295-b155-7fffbbbddee1" />

### Controls
<img width="1004" alt="Controls" src="https://github.com/user-attachments/assets/e16cce54-e177-42e6-9e38-367179f0eb7f" />

### Train editor
<img width="1527" alt="Train editor" src="https://github.com/user-attachments/assets/d2651d75-3c3c-4e80-8543-13d7d5f427b2" />

### Train CLI
<img width="619" height="915" alt="image" src="https://github.com/user-attachments/assets/16718614-c195-4cce-afa5-4b4569f9a621" />


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

## Architecture documentation (arc42)

Full architecture documentation lives in **[docs/arc42/](docs/arc42/)**, following the
[arc42](https://arc42.de/overview/) template. Good entry points:

| If you want to… | Read |
|---|---|
| Understand *why* it looks like this | [§4 Solution Strategy](docs/arc42/04-solution-strategy.md) |
| Find where code belongs | [§5 Building Block View](docs/arc42/05-building-block-view.md) |
| Know how discovery / driving / shutdown actually work | [§6 Runtime View](docs/arc42/06-runtime-view.md) |
| Deploy to a Pi, or understand the CI | [§7 Deployment View](docs/arc42/07-deployment-view.md) |
| Avoid re-opening a settled decision | [§9 Architecture Decisions](docs/arc42/09-architecture-decisions.md) |
| See what's knowingly unfinished | [§11 Risks and Technical Debt](docs/arc42/11-risks-and-technical-debt.md) |

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

## Architekturdokumentation (arc42)

Die vollständige Architekturdokumentation liegt in **[docs/arc42/](docs/arc42/)** und folgt der
[arc42](https://arc42.de/overview/)-Vorlage (auf Englisch, wie Code und CLI). Einstiegspunkte:

| Wenn du … willst | Lies |
|---|---|
| verstehen, *warum* es so aussieht | [§4 Solution Strategy](docs/arc42/04-solution-strategy.md) |
| wissen, wo Code hingehört | [§5 Building Block View](docs/arc42/05-building-block-view.md) |
| Discovery / Fahren / Shutdown im Detail | [§6 Runtime View](docs/arc42/06-runtime-view.md) |
| auf einen Pi deployen oder die CI verstehen | [§7 Deployment View](docs/arc42/07-deployment-view.md) |
| keine bereits getroffene Entscheidung neu aufrollen | [§9 Architecture Decisions](docs/arc42/09-architecture-decisions.md) |
| sehen, was bewusst offen ist | [§11 Risks and Technical Debt](docs/arc42/11-risks-and-technical-debt.md) |

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
