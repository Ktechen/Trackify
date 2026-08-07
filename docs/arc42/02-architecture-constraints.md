# 2. Architecture Constraints

Constraints are grouped by how negotiable they are: physics and third-party reality first, then
technology choices, then the rules the project imposes on itself.

## 2.1 Technical constraints (not negotiable)

| ID | Constraint | Consequence |
|---|---|---|
| TC-1 | Hubs speak the **LEGO Wireless Protocol v3** over BLE GATT. The protocol is fixed by LEGO. | All control is expressed as LWP messages; the vendored spec lives in `docs/lego-ble-wireless-protocol-docs` (submodule). |
| TC-2 | **BLE is per-platform.** Android/iOS use the OS BLE stack, Windows uses WinRT, Linux uses BlueZ over D-Bus. There is no portable .NET BLE API. | One port (`ILegoService`), four implementations, selection at DI time — [ADR-03](09-architecture-decisions.md#adr-03-ilegoservice-as-the-single-ble-seam). |
| TC-3 | A hub is a **single-connection device**. Two clients cannot drive the same hub simultaneously. | Server mode is an *either/or* with Direct mode, not an overlay — [ADR-09](09-architecture-decisions.md#adr-09-runtime-directserver-switch-instead-of-a-restart). |
| TC-4 | **BlueZ needs a powered radio and an LE-filtered scan** or BLE-only hubs never appear; a fresh `StartDiscovery` does not re-announce already-cached devices. | `BlueZPoweredUpBluetoothAdapter` powers the radio, sets `Transport=le`, and enumerates `GetDevicesAsync()` at scan start. |
| TC-5 | Bluetooth on Android requires **runtime permissions** tied to an `Activity`. | `AndroidBluetoothPermissionService` lives in the app head (it needs the `Activity`), behind the `IBluetoothPermissionService` port. |
| TC-6 | **iOS builds require macOS.** The `net10.0-ios` head cannot be produced on Windows or Linux. | `Trackify.Application` conditions its TFM set on the *build host* OS — [ADR-05](09-architecture-decisions.md#adr-05-host-conditioned-multi-targeting-of-trackifyapplication). |
| TC-7 | The **WASM head has no Bluetooth**; desktop (Skia) has no supported BLE stack either. | Those heads get `UnsupportedLegoService` and rely on Server mode for real control. |
| TC-8 | A Docker container has no radio of its own — BLE goes through the **host's `bluetoothd`**. | `docker-compose.yml` uses `network_mode: host` and mounts `/var/run/dbus`. |

## 2.2 Technology constraints (chosen, but now load-bearing)

| ID | Constraint | Notes |
|---|---|---|
| TE-1 | **.NET 10**; shared libraries multi-target `net10.0;net9.0`. | SDK pinned in `global.json` (`9.0.100` + `rollForward: latestMajor`). |
| TE-2 | **Uno Platform** for the app, version pinned via `Uno.Sdk` in `global.json` — *not* in package props. | Five heads: `android`, `ios`, `browserwasm`, `desktop`, `windows10.0.19041.0`. |
| TE-3 | **SharpBrick.PoweredUp 5.0.2** implements LWP; `.Mobile` and `.WinRT` variants provide the app transports. | Vendor pinning cascades: see TE-4. |
| TE-4 | **`Plugin.BLE` is pinned to exactly `3.0.0`** — the version `SharpBrick.PoweredUp.Mobile 5.0.2` was compiled against. | A newer Plugin.BLE changes signatures SharpBrick calls → runtime `MissingMethodException` on connect. [ADR-10](09-architecture-decisions.md#adr-10-pin-pluginble-to-300). |
| TE-5 | **EF Core 9.x + SQLite** for persistence; schema created with `EnsureCreated()`, no migrations. | Changing an entity's shape means deleting the dev `trackify.db`. |
| TE-6 | **Spectre.Console.Cli** for the CLI, bridged to `Microsoft.Extensions.DependencyInjection` by the `…Extensions.DependencyInjection` package. | No hand-written `ITypeRegistrar`. |
| TE-7 | **Serilog** is the logging backend behind source-generated `[LoggerMessage]` call sites. | Levels/sinks from `appsettings.json`; the Console sink assembly is passed explicitly so single-file publishes still bind. |
| TE-8 | **Central Package Management** — versions only in `Directory.Packages.props`, never in a csproj. | |
| TE-9 | The LAN backend is **ASP.NET Core via `FrameworkReference`**, not NuGet packages. | Referencing `Microsoft.Extensions.*` as packages alongside it trips `NU1510`. |

## 2.3 Organizational and process constraints

| ID | Constraint |
|---|---|
| OC-1 | **Single maintainer, hobby cadence.** Anything that only works because someone remembers it will break; conventions must be machine-checked. |
| OC-2 | **CI runs on hosted GitHub runners.** The Uno app's five heads cannot be restored on one runner, so `ci.yml` gates the shared core + CLI + tests only; the Android head is covered by `android-apk.yml`. |
| OC-3 | **Hardware verification is manual.** BLE cannot run in CI or in an agent environment — real behaviour is confirmed on a phone and on a Raspberry Pi. |
| OC-4 | **SonarCloud + CodeQL** run on every PR. `docs/**` (the vendored LEGO submodule) is excluded — third-party HTML/JS the project neither owns nor fixes. |
| OC-5 | The vendored LEGO protocol documentation is a **git submodule**; it is read-only reference material. |

## 2.4 Self-imposed conventions (enforced by the build)

These are constraints by choice — they exist so the architecture survives OC-1.

| ID | Rule | Enforcement |
|---|---|---|
| SC-1 | **Dependencies point inward only**: `Domain ← Application ← Infrastructure ← front-ends`. | `LayerTrainDependencyTests` (NetArchTest) in CI |
| SC-2 | **Front-ends never touch Domain entities** — they work with `TrainDto`. | `Cli_never_touches_the_domain_entity_namespace` |
| SC-3 | **Namespace matches folder** and is **file-scoped**. `Platforms/**` is exempt (Uno convention namespaces). | `IDE0130` / `IDE0161` as **errors** |
| SC-4 | **Warnings are errors**; code-style analyzers run during build. The test project opts out (xUnit analyzers are strict). | `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild` |
| SC-5 | **One top-level type per file**, named after the type. | Review |
| SC-6 | **Type-name suffix per folder**: `Services/`→`*Service`, `ViewModels/`→`*ViewModel`, `Behaviors/`→`*Behavior`, `Widgets/`→`*Widget`. | Review |
| SC-7 | **No page code-behind** beyond `InitializeComponent()` (one deliberate exception: the responsive master–detail layout in `MainPage.xaml.cs`). Use attached behaviors instead of event handlers. | Review |
| SC-8 | **No silent failures** — never an empty `catch`. Log and return a failure, or rethrow. Unhandled errors are caught globally per front-end. | Review; see [§8.6](08-crosscutting-concepts.md#86-error-handling) |
| SC-9 | **Domain stays dependency-free** — no logging, no EF, no BLE, no UI. | `Domain_is_pure_and_free_of_infrastructure_frameworks` |
| SC-10 | Every project carries a `GlobalUsings.cs` with its genuinely common namespaces. | Review |

## 2.5 Conventions for people, not compilers

| ID | Rule |
|---|---|
| UC-1 | **App UI language is German** (labels, dialogs, user-facing error text). **CLI output is English.** Code, comments and this documentation are English. |
| UC-2 | The folder is `Pages`, never "Screens". `Components/` = page-specific sections inheriting the page `DataContext`; `Widgets/` = reusable atoms exposing `DependencyProperty`s. |
| UC-3 | Converters are registered **once** globally in `Styles/Converters.xaml`; design tokens live in `Styles/DesignTokens.xaml`. |
| UC-4 | Classic `{Binding}` views carry a design-time `d:DataContext="{d:DesignInstance …}"` so binding paths resolve in the IDE. |
