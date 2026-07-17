# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

**Trackify** configures and controls **LEGO Powered Up** train hubs over **Bluetooth LE**, directly on-device — there is **no server/backend**. BLE talks the [LEGO Wireless Protocol (LWP) v3](https://lego.github.io/lego-ble-wireless-protocol-docs/) via [SharpBrick.PoweredUp](https://github.com/sharpbrick/powered-up).

It's a **Clean Architecture** solution with two front-ends over one shared core. All projects live under `Source/` (the "Source" solution folder):

- `Trackify.Domain` — pure entities (`TrainConfig`, `TrackSegmentConfig`), enums, `SpeedFunction` math. Only dependency is the DI abstractions (contract-only), for its `AddTrackifyDomain()` entry point.
- `Trackify.Application` — ports (`ILegoService`, `IBluetoothPermissionService`, `ITrainStore`) + `DiscoveredHub`, `LegoinoCatalog`, `TrainControlService`, `LwpAddressing`. Depends only on Domain; UI/transport-agnostic.
- `Trackify.Infrastructure` — `JsonTrainStore` (shared JSON persistence) **plus** the BlueZ onboard-radio `ILegoService` under `Ble/` (`Linux.Bluetooth` + SharpBrick). Referenced **only by the CLI** — it carries the Linux BLE stack, so the Uno app must not reference it.
- `Trackify` — the [Uno Platform](https://platform.uno) app (single project, multi-head = App / HMI / **Web = WASM head**). References **Domain + Application only**; hosts its own mobile/WinRT `ILegoService` impls in-project.
- `Trackify.Cli` — a **Spectre.Console.Cli** console app to deploy on a Linux server / Raspberry Pi.
- `Trackify.Tests` — xUnit (pure-logic + store round-trip).

- .NET 10, Uno.Sdk `6.5.36` (pinned in `global.json` — update Uno there, not in package props).
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
dotnet test  Source/Trackify.Tests/Trackify.Tests.csproj
dotnet run --project Source/Trackify.Cli -- --help        # trackify discover | list | connect | drive | stop | color
```

**The Uno Skia-rendered UI cannot be screenshotted in this environment** (GPU surface comes back blank; the WASM canvas render-loop times out the browser tool). So UI work cannot be pixel-verified here. Likewise **BlueZ BLE cannot run here** — the Linux transport compiles on Windows but only works on a Raspberry Pi. Verification is: **build green on all four buildable heads + `dotnet test` + desktop launch smoke test + CLI `--help`/`list`**; the user confirms visuals on a real device and BLE on a Pi.

## Architecture

**Per-layer DI.** Each core layer owns its registration via a `DependencyInjection.cs` extension: `AddTrackifyDomain()` (no-op — Domain is pure), `AddTrackifyApplication()` (registers `TrainControlService`), `AddTrackifyInfrastructure(storePath?)` (registers `ITrainStore`→`JsonTrainStore` + `AddLinuxLego()`). A composition root just chains them: the **CLI** calls all three; the **Uno app** calls Domain + Application (it doesn't reference Infrastructure) and adds its own platform `ILegoService` in `RegisterLegoService`.

**`ILegoService`** (in `Trackify.Application`) is the BLE seam every front-end shares. `TrainControlService` (Application) is the shared control logic (discover/connect/speed-debounce/LED) over a pure `TrainConfig`. Per-transport `ILegoService` implementations are chosen at DI time:
- Android/iOS → `DirectLegoService` (SharpBrick `.Mobile` / Plugin.BLE) — **in the Uno project**, `App.xaml.cs` → `RegisterLegoService`.
- Windows → `WindowsLegoService` (SharpBrick `.WinRT`) — in the Uno project.
- desktop/wasm → `UnsupportedLegoService` (no BLE stack) — in the Uno project.
- Linux/Pi → `BlueZLegoService` (SharpBrick + `Linux.Bluetooth`, onboard radio) — in `Trackify.Infrastructure/Ble/`, wired by the CLI via `LinuxLegoServiceExtensions.AddLinuxLego()`.

The Uno mobile/WinRT services stay in the Uno project on purpose: they need Uno.Sdk multi-targeting + the Android permission `Activity`. The app is a composition root, so it legitimately provides its own platform infrastructure. The BlueZ transport lives in `Trackify.Infrastructure/Ble/` (a folder, not a separate project) and is used only by the CLI — the Uno app deliberately does **not** reference `Trackify.Infrastructure`, so `Linux.Bluetooth`/`Tmds.DBus` never reach the Android/iOS/wasm/Windows heads.

- **`#if LINUX` guard is DI-only.** The BlueZ implementation files (`Ble/BlueZ*.cs`, `LwpCommands`, `ConnectedHub`) compile unconditionally — the BLE packages (`SharpBrick.PoweredUp`, `Linux.Bluetooth`) are always referenced by `Trackify.Infrastructure`. The single `#if LINUX` lives in `LinuxLegoServiceExtensions.AddLinuxLego`, which registers the real `BlueZLegoService` when `LINUX` is defined and a no-op `UnsupportedLegoService` otherwise. There is no built-in Linux compile symbol for `net10.0`, so `LINUX` is defined via the MSBuild property `TrackifyLinux` — auto-set when building on a Linux host, or forced with `-p:TrackifyLinux=true`.

- **`SharpBrick` command building is NOT pure** — `LwpProtocol.cs` (Uno app) and `LwpCommands.cs` (Linux lib) build SharpBrick typed messages (StartPower, SetRgbColor, connect-with-retry). This deliberately duplicates a few tiny per-transport helpers rather than putting SharpBrick in the shared Application layer. The genuinely pure bits (RGB-LED port table, MAC format/parse) live once in `Application/Lego/LwpAddressing.cs`.
- **Uno app** (`Source/Trackify/`): `Presentation/` (MVVM, folder-per-concern, no business logic in pages), `Services/` (the platform `ILegoService` impls + `NativeBluetooth` permission glue), `Helpers/SpeedCurve.cs` (builds the presentation-only `SpeedProfileGraph` path data), `Models/Trains/` (`Train`/`TrackSegment` — currently `ObservableObject`; **Phase 4 will make them thin wrappers over the Domain configs**).
- **Navigation & DI** — Uno.Extensions Hosting/Navigation. Routes/ViewMaps in `App.xaml.cs` → `RegisterRoutes`.
- **CLI** (`Source/Trackify.Cli/`): Spectre.Console.Cli; `Program.cs` builds the DI container (`AddLinuxLego` + `JsonTrainStore` + `TrainControlService`) bridged to Spectre via `TypeRegistrar`/`TypeResolver`. Commands under `Commands/`. Store path overridable with `TRACKIFY_STORE`.

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
- **Logging** is high-performance source-generated `[LoggerMessage]`: each project that logs has an internal static partial `Log` class (`Logging/Log.cs`) with event-id'd messages, called with an injected `ILogger`. The backend is **Serilog** (`AddSerilog` — console in the app + CLI). Services take an optional `ILogger<T>` defaulting to `NullLogger` so tests/manual construction need no logger. **Domain stays dependency-free — no logging there.**
- **Tests** (`Trackify.Tests`) are foldered by layer — `Domain/`, `Application/`, `Infrastructure/`, `Cli/` — with reusable test doubles under `Fakes/`. Internal CLI helpers are reachable via `InternalsVisibleTo`.
- UI language / labels are **German** (app). CLI output is English.

## Gotchas / hard-won facts

- **`Plugin.BLE` is pinned to `3.0.0`** — it must match the version `SharpBrick.PoweredUp.Mobile 5.0.2` was compiled against. A newer Plugin.BLE changes signatures SharpBrick calls → runtime `MissingMethodException` on connect. Its `net7.0-android33.0` asset is consumable by `net10.0-android`.
- **Connect uses a bounded retry** catching `NullReferenceException`/`ArgumentNullException` — works around an unfixed null-deref in SharpBrick's `BluetoothKernel.ConnectAsync` ([sharpbrick/powered-up#188](https://github.com/sharpbrick/powered-up/issues/188)).
- **`CS7064`** (wasm favicon) is kept as a warning (not error) via `WarningsNotAsErrors` — Uno.Resizetizer can generate `favicon.ico` after the compiler first references it on clean builds.
- Mobile safe-area (status bar / gesture nav / notch) is handled with the Uno `Toolkit` feature: `utu:SafeArea.Insets` on the page header (`Top`) and content (`Bottom`). Adding a mobile screen? Apply the same so system bars don't overlap.
