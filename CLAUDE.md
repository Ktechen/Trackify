# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Planned — MCP server ([issue #1](https://github.com/Ktechen/Trackify/issues/1), NOT yet implemented)

Agreed shape (plan only so far): expose train control over **MCP (Model Context Protocol)** so an AI
client can drive trains, using **HTTP/SSE** transport, with the MCP pieces living **in
`Trackify.Infrastructure`** (per the issue's "Add MCP to Infrastructure").
- Packages: official `ModelContextProtocol` C# SDK + `ModelContextProtocol.AspNetCore` (HTTP/SSE host).
- `Trackify.Infrastructure/Mcp/`: MCP tool classes wrapping `TrainControlService` + `ITrainRepository`
  (`list_trains`, `discover_hubs`, `drive_train`, `stop_train`, `set_color`) + an `AddTrackifyMcp()` DI extension.
- Host: a thin server front-end (a `trackify serve` command or a small ASP.NET host) that chains
  `AddTrackifyDomain/Application/Infrastructure` + `AddTrackifyMcp()` and maps the MCP SSE endpoint —
  "server-based logic like the CLI" (same use-cases, over the network instead of argv).
- Layer note: an inbound MCP server is normally a front-end; per the issue it sits in Infrastructure,
  with the network host as the composition root. Runtime verification needs an MCP client + a Pi (BlueZ).

## What this is

**Trackify** configures and controls **LEGO Powered Up** train hubs over **Bluetooth LE**, directly on-device — there is **no server/backend**. BLE talks the [LEGO Wireless Protocol (LWP) v3](https://lego.github.io/lego-ble-wireless-protocol-docs/) via [SharpBrick.PoweredUp](https://github.com/sharpbrick/powered-up).

It's a **Clean Architecture** solution with two front-ends over one shared core. All projects live under `Source/` (the "Source" solution folder):

- `Trackify.Domain` — pure entities (`Train`, `TrackSegment`), enums, `SpeedFunction` math. Only dependency is the DI abstractions (contract-only), for its `AddTrackifyDomain()` entry point.
- `Trackify.Application` — ports + use-cases (`ILegoService`/`ITrainControlService`/`ITrainService`/`ITrainRepository` + `DiscoveredHubDto`, `LegoinoCatalog`, `TrainControlService`, `TrainService`, `TrainDto`/`TrainMapping`, `LwpAddressingMapping`) **plus the per-platform `ILegoService` transports under `Services/`** (`DirectLegoService` android/ios, `WindowsLegoService`) wired by per-head `Add{Android,Ios,Windows}Lego` extensions (mirroring the CLI's `AddLinuxLego`). **Multi-targeted per BUILD host** (Windows: `+net10.0-android;net10.0-windows`; macOS: `+net10.0-android;net10.0-ios`; Linux: plain `net10.0` only). The plain `net10.0` flavor is transport-free (CLI/Pi carries BlueZ instead).
- `Trackify.Infrastructure` — `SqliteTrainRepository` (EF Core + **SQLite** persistence, `trackify.db`; enums stored as names) **plus** the BlueZ onboard-radio `ILegoService` under `Ble/` (`Linux.Bluetooth` + SharpBrick). Referenced **only by the CLI** — it carries the Linux BLE stack, so the Uno app must not reference it.
  - **Repository pattern**: entities derive from `Domain/Common/BaseEntity` (Guid `Id` + `DateCreated`/`DateUpdated`); repositories derive from the generic `Infrastructure/Persistence/BaseRepository<T>` (default EF CRUD — `GetById/GetAll/Find/Add/AddRange/Update/Delete` over an `IDbContextFactory`) and implement a per-entity port that extends `Application/Common/IBaseRepository<T>` (e.g. `ITrainRepository : IBaseRepository<Train>`; `SqliteTrainRepository` adds only the default-db-path helper). Schema is created with `EnsureCreated()` (no migrations) — **changing an entity's shape means deleting the dev `trackify.db`** (or adding EF migrations), since `EnsureCreated` won't migrate an existing file.
- `Trackify` — the [Uno Platform](https://platform.uno) app (single project, multi-head = App / HMI / **Web = WASM head**). References **Domain + Application only**; the BLE transports flow from Application per head (`AddTrackifyApplication` calls the right `Add{Android,Ios,Windows}Lego`). The app's own `Services/` holds only `AndroidBluetoothPermissionService` (needs the `Activity`) + the desktop/wasm no-op registration.
- `Trackify.Cli` — a **Spectre.Console.Cli** console app to deploy on a Linux server / Raspberry Pi (plain `net10.0` → BlueZ via `AddLinuxLego`'s runtime check). On a Windows dev box it additionally targets `net10.0-windows10.0.19041.0`, where the `WINDOWS` symbol makes Application compile in `AddWindowsLego` (WinRT) — so `discover`/`drive` work on Windows too (run that TFM; the plain `net10.0` flavor has no Windows transport). `RuntimeIdentifiers=linux-arm64` (on the net10.0 flavor) so arm64 restore/publish works from Rider.
- `Trackify.Tests` — xUnit (pure-logic + store round-trip).

- .NET 10, Uno.Sdk `6.5.36` (pinned in `global.json`; SDK pinned to `9.0.100` + `rollForward: latestMajor` → uses the newest installed major, net10 heads require the .NET 10 SDK). Update Uno in `global.json`, not in package props.
- **CI** (GitHub Actions, `.github/workflows/`): `ci.yml` is the pre-merge gate on PRs to `master` — an ubuntu job builds the CLI + shared core and runs the tests. The Uno app is deliberately not built in CI (its 5 heads need workloads + macOS/Windows even with `-f <head>`, because restore imports workloads for all TFMs); the Android head is covered by `android-apk.yml`. `android-apk.yml` builds the Android APK on windows-latest (JDK 17 + `android` workload, `-f net10.0-android`) on tag `v*` or manual dispatch; artifact `trackify-apk`. Both provision the .NET 8/9/10 SDKs.
- Uno heads: `net10.0-android`, `net10.0-ios`, `net10.0-browserwasm`, `net10.0-desktop`, `net10.0-windows10.0.19041.0`. The shared libs + CLI are plain `net10.0`.
- Central Package Management (`Directory.Packages.props`) — add versions there, reference without a version in the csproj.
- `TreatWarningsAsErrors=true` and `EnforceCodeStyleInBuild=true` are on (repo-wide via `Directory.Build.props`). **The build fails on warnings and on code-style violations.** (The test project opts out of both, since xUnit analyzers are strict.)

## Build & verify

```bash
# Build one Uno head (do this per head when verifying an app change)
dotnet build Source/Trackify/Trackify.csproj -f net10.0-desktop

# The four heads that build in this environment (iOS needs a Mac):
#   net10.0-android  net10.0-desktop  net10.0-browserwasm  net10.0-windows10.0.19041.0

# Desktop launch smoke test (no XAML/DI exceptions on startup)
dotnet run --project Source/Trackify/Trackify.csproj -f net10.0-desktop

# Shared core / CLI / tests
dotnet build Source/Trackify.Cli/Trackify.Cli.csproj      # CLI compiles here; real BLE only on a Pi
dotnet test  Test/Trackify.Tests/Trackify.Tests.csproj
dotnet run --project Source/Trackify.Cli -- --help        # trackify discover | list | connect | drive | stop | color
```

CLI deployment (Pi publish — no build flags; `trains.json`/store schema, systemd autostart unit, **Docker**) is documented in `Source/Trackify.Cli/ReadMe.md`. Docker: `Source/Trackify.Cli/Dockerfile` (multi-stage, repo root as context) + root `docker-compose.yml` (host network + `/var/run/dbus` mount for the host's BlueZ, `stop_signal: SIGINT` for clean shutdown).

**The Uno Skia-rendered UI cannot be screenshotted in this environment** (GPU surface comes back blank; the WASM canvas render-loop times out the browser tool). So UI work cannot be pixel-verified here. Likewise **BlueZ BLE cannot run here** — the Linux transport compiles on Windows but only works on a Raspberry Pi. Verification is: **build green on all four buildable heads + `dotnet test` + desktop launch smoke test + CLI `--help`/`list`**; the user confirms visuals on a real device and BLE on a Pi.

## Architecture

**Per-layer DI.** Each core layer owns its registration via a `DependencyInjection.cs` extension: `AddTrackifyDomain()` (no-op — Domain is pure), `AddTrackifyApplication()` (registers `ITrainControlService`/`ITrainService` **and, per platform via `#if`, the matching `Add{Android,Ios,Windows}Lego`**), `AddTrackifyInfrastructure(storePath?)` (registers `ITrainRepository`→`SqliteTrainRepository` + `AddLinuxLego()`). A composition root chains them: the **CLI** calls all three; the **Uno app** calls Domain + Application and adds only its own bits in `RegisterLegoService` (Android permission service; desktop/wasm no-op).

**`ILegoService`** (in `Trackify.Application`) is the BLE seam; `TrainControlService` is the shared control logic over a `TrainDto`. Per-transport implementations, each behind its own `Add…Lego` helper:
- Android/iOS → `DirectLegoService` (SharpBrick `.Mobile` / Plugin.BLE) — `Trackify.Application/Services/`, via `AddAndroidLego`/`AddIosLego`.
- Windows → `WindowsLegoService` (SharpBrick `.WinRT`) — `Trackify.Application/Services/`, via `AddWindowsLego`.
- desktop/wasm → `UnsupportedLegoService` (`Trackify.Application/Services/`), registered by the app's `RegisterLegoService`.
- Linux/Pi → `BlueZLegoService` (SharpBrick + `Linux.Bluetooth`) — `Trackify.Infrastructure/Ble/`, via `AddLinuxLego`.

- **Two selection styles, deliberately.** `AddLinuxLego` is a **runtime** check (`if (OperatingSystem.IsLinux())`) because the BlueZ types compile on every TFM. `AddAndroidLego`/`AddIosLego`/`AddWindowsLego` are **compile-time `#if`** per head, because `DirectLegoService`/`WindowsLegoService` reference SDK packages (Plugin.BLE, SharpBrick `.WinRT`) that only exist on those TFMs — they don't compile elsewhere, so a runtime check is impossible there.

- **`SharpBrick` command building is NOT pure** — `LwpCommands.cs` (`Trackify.Infrastructure/Ble/`) builds SharpBrick typed messages (StartPower, SetRgbColor, connect-with-retry) for the BlueZ transport. The genuinely pure bits (RGB-LED port table, MAC format/parse) live once in `Application/Lego/LwpAddressing.cs`.
- **Uno app** (`Source/Trackify/`): `Presentation/` (MVVM, folder-per-concern, no business logic in pages), `Services/` (the platform `ILegoService` impls + `NativeBluetooth` permission glue), `Helpers/SpeedCurve.cs` (builds the presentation-only `SpeedProfileGraph` path data), `Models/Trains/` (`Train`/`TrackSegment` — currently `ObservableObject`; **Phase 4 will make them thin wrappers over the Domain configs**).
- **Navigation & DI** — Uno.Extensions Hosting/Navigation. Routes/ViewMaps in `App.xaml.cs` → `RegisterRoutes`.
- **CLI** (`Source/Trackify.Cli/`): Spectre.Console.Cli; `Program.cs` composes the layers (`AddTrackifyDomain/Application/Infrastructure` + Serilog) bridged to Spectre via the `Spectre.Console.Cli.Extensions.DependencyInjection` package (`DependencyInjectionRegistrar` — no hand-written registrar). Commands under `Commands/`, settings under `Commands/Settings/`. Store (SQLite) path overridable with `TRACKIFY_STORE`. Single `net10.0`; hub control only on Linux/Pi (BlueZ), a no-op elsewhere.

## Conventions (enforced)

- **Namespace must match folder** (`IDE0130` = error) and be **file-scoped** (`IDE0161` = error). `Platforms/**` is exempt (Uno convention namespaces like `Trackify.Droid`). Other code-style rules are downgraded to suggestions so they don't flood the build.
- **Type-name suffix per folder:** `Services/` → `*Service`, `Presentation/ViewModels/` → `*ViewModel`, `Presentation/Behaviors/` → `*Behavior`, `Presentation/Widgets/` → `*Widget`.
- **Folder is `Pages`, not "Screens".**
- **`Components/`** = page-specific composed sections that inherit the page's `DataContext`. **`Widgets/`** = reusable atoms that expose `DependencyProperty`s (e.g. `SpeedProfileWidget.Graph`).
- **No page code-behind** beyond `InitializeComponent()`, except the deliberate responsive master-detail layout in `Pages/MainPage.xaml.cs`. Use attached behaviors (e.g. `Behaviors/TappedCommandBehavior`) instead of `*_Tapped` handlers.
- **MVVM** with CommunityToolkit.Mvvm: field-based `[ObservableProperty]` and `[RelayCommand]`. (`MVVMTK0045` partial-property advice is intentionally suppressed.)
- Converters are registered **once** globally in `Styles/Converters.xaml` (merged in `App.xaml`); don't re-declare per page. Design tokens/styles live in `Styles/DesignTokens.xaml`.
- Classic `{Binding}` views carry a design-time `d:DataContext="{d:DesignInstance ...}"` (with `mc:Ignorable="d"`) so binding paths resolve in the IDE. This is design-time only. Add one when creating a new view/data-template.
- **One top-level type per file**, named after the type (records, interfaces, enums and small helpers each get their own file — e.g. each `*Option` DTO, each converter, each BlueZ wrapper). Private nested implementation detail is fine only when it can't be top-level.
- **Every project has a `GlobalUsings.cs`** carrying its genuinely-common namespaces (e.g. Application: `Trackify.Application.Lego` + Domain namespaces; CLI: Spectre + `Trackify.Application.Trains`; Tests: `Xunit`). Don't re-import those per file.
- **Logging** is high-performance source-generated `[LoggerMessage]`: each project that logs has an internal static partial `Log` class (`Logging/Log.cs`) with event-id'd messages, called with an injected `ILogger`. The backend is **Serilog** (`AddSerilog` — console in the app + CLI). `ILogger<T>` is a **required** ctor dependency (every composition root registers logging; DI factories use `GetRequiredService`); tests pass `NullLogger<T>.Instance` explicitly. **Domain stays dependency-free — no logging there.**
- **Tests** (`Trackify.Tests`) are foldered by layer — `Domain/`, `Application/`, `Infrastructure/`, `Cli/`, `Architecture/` — with reusable test doubles under `Fakes/`. Internal CLI helpers are reachable via `InternalsVisibleTo`.
- **Architecture tests** (`Architecture/LayerDependencyTests.cs`, `NetArchTest.Rules`) enforce the dependency rule in CI: Domain depends on no other layer and no infra framework (EF Core/SharpBrick); Application depends on neither Infrastructure nor a front-end nor EF Core; Infrastructure depends on no front-end; and the **DTO boundary** — the CLI must not depend on the Domain entity namespace `Trackify.Domain.Trains` (it works only with `TrainDto`). The test project references the CLI solely so this last rule is checkable.
- UI language / labels are **German** (app). CLI output is English.

## Gotchas / hard-won facts

- **`Plugin.BLE` is pinned to `3.0.0`** — it must match the version `SharpBrick.PoweredUp.Mobile 5.0.2` was compiled against. A newer Plugin.BLE changes signatures SharpBrick calls → runtime `MissingMethodException` on connect. Its `net7.0-android33.0` asset is consumable by `net10.0-android`.
- **Connect uses a bounded retry** catching `NullReferenceException`/`ArgumentNullException` — works around an unfixed null-deref in SharpBrick's `BluetoothKernel.ConnectAsync` ([sharpbrick/powered-up#188](https://github.com/sharpbrick/powered-up/issues/188)).
- **`CS7064`** (wasm favicon) is kept as a warning (not error) via `WarningsNotAsErrors` — Uno.Resizetizer can generate `favicon.ico` after the compiler first references it on clean builds.
- Mobile safe-area (status bar / gesture nav / notch) is handled with the Uno `Toolkit` feature: `utu:SafeArea.Insets` on the page header (`Top`) and content (`Bottom`). Adding a mobile screen? Apply the same so system bars don't overlap.
