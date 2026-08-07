# 12. Glossary

## Domain terms

| Term | Definition |
|---|---|
| **Hub** | A LEGO Powered Up smart brick — the battery box with a Bluetooth radio, motor ports and a built-in RGB LED. Modelled by `HubType` (e.g. `PoweredUpHub`) |
| **Train** | A saved *configuration*, not a physical object: name, hub type, address, LED colour, port assignments, speed, and accel/brake curves. Deliberately holds **no runtime state** ([§8.1](08-crosscutting-concepts.md#81-domain-model)) |
| **Track segment** | One configured piece of layout: type, max speed, direction, accel/brake functions, sensor and sensor action. Geometry and labels belong to presentation |
| **Port A / Port B** | The hub's two device ports. Port A (`MotorPort = 0`) carries the train motor driven by the speed slider |
| **Power / speed** | A signed percentage sent to a motor: `1..100` forward, `-1..-100` reverse, `0` = stop (float, coasts), `127` = stop (brake) |
| **Speed function** | The `f(x)` curve over `x ∈ [0,1]` describing acceleration or braking: `Linear`, `EaseIn`, `EaseOut`, `SCurve`, `Exponential`, or `Custom` (a user-entered formula) |
| **Hub key** | The string a hub is addressed by: the platform device id captured during discovery (`HubId`), falling back to the typed BLE address. Android accepts a MAC; iOS requires discovery first |
| **Direct mode** | The app drives hubs with the device's **own** radio. The default |
| **Server mode** | The app sends commands to a Trackify backend (typically on a Pi) that owns the radio |
| **Auto-pilot** | `trackify auto` — the unattended loop that re-applies every saved train's configuration on an interval and reconnects dropped hubs |
| **Sweep** | One iteration of the auto-pilot: re-read the store, then connect/LED/speed each target train |
| **Discovery** | Scanning for BLE advertisements from nearby hubs. It has no fixed timeout — the caller cancels |

## Protocol and radio terms

| Term | Definition |
|---|---|
| **BLE** | Bluetooth Low Energy — the radio protocol all hubs speak |
| **LWP (v3)** | [LEGO Wireless Protocol](https://lego.github.io/lego-ble-wireless-protocol-docs/) version 3, the message format over BLE. Vendored in `docs/lego-ble-wireless-protocol-docs` (submodule) |
| **GATT** | Generic Attribute Profile — the BLE service/characteristic model LWP messages travel over |
| **Advertisement** | The broadcast a powered-on hub emits; its manufacturer data identifies the hub type |
| **`StartPower`** | The LWP message that drives a motor at a given power |
| **`SetRgbColor`** | The LWP message that sets the hub's built-in RGB LED |
| **BlueZ** | The Linux Bluetooth stack. Trackify talks to its `bluetoothd` daemon over D-Bus |
| **D-Bus** | The Linux IPC bus. `org.bluez` is the BlueZ service name; container deployments mount `/var/run/dbus` |
| **`ServicesResolved`** | The BlueZ device property that signals GATT services are enumerated. Trackify waits for this, not merely `Connected` — GATT lookups race and return null otherwise |
| **`rfkill`** | The Linux utility that soft-blocks radios. A soft-blocked adapter silently scans and connects nothing |
| **LE transport filter** | `SetDiscoveryFilter{Transport = le}` — required, because BlueZ's default "auto" (BR/EDR + LE) routinely misses BLE-only hubs |
| **WinRT / CoreBluetooth** | The Windows and iOS Bluetooth APIs, reached via SharpBrick `.WinRT` and Plugin.BLE respectively |

## Technical and .NET terms

| Term | Definition |
|---|---|
| **Clean Architecture** | The layering used here: `Domain ← Application ← Infrastructure ← front-ends`, dependencies inward only |
| **Port** | An interface owned by the Application layer and implemented further out — `ILegoService`, `ITrainRepository`, `IBluetoothPermissionService` |
| **Composition root** | The single place a host wires DI: `Program.cs` (CLI), `App.xaml.cs` (Uno), `TrackifyServer.RunAsync` (backend) |
| **DTO** | Data Transfer Object. `TrainDto` is the front-end-facing shape; Domain entities never cross the Application boundary ([ADR-07](09-architecture-decisions.md#adr-07-front-ends-see-traindto-never-domain-entities)) |
| **ADR** | Architecture Decision Record — see [§9](09-architecture-decisions.md) |
| **arc42** | The architecture documentation template this document follows |
| **TFM** | Target Framework Moniker, e.g. `net10.0-android`. Which TFMs `Trackify.Application` builds depends on the **build host** ([ADR-05](09-architecture-decisions.md#adr-05-host-conditioned-multi-targeting-of-trackifyapplication)) |
| **RID** | Runtime Identifier, e.g. `linux-arm64` — the target platform for a publish |
| **Head** | One Uno Platform target of the single app project: `android`, `ios`, `browserwasm`, `desktop`, `windows10.0.19041.0` |
| **Self-contained publish** | A build that bundles the .NET runtime, so the Pi needs nothing installed |
| **CPM** | Central Package Management — package versions live only in `Directory.Packages.props` |
| **`EnsureCreated()`** | The EF Core call that creates a schema without migrations. It will **not** migrate an existing database ([R-5](11-risks-and-technical-debt.md#r-5-the-ensurecreated-schema-trap-low-impact-high-annoyance)) |
| **NetArchTest** | The library used to assert the layer dependency rules as unit tests |
| **`[LoggerMessage]`** | The .NET source generator producing allocation-free logging call sites ([ADR-12](09-architecture-decisions.md#adr-12-source-generated-loggermessage-over-serilog)) |
| **Refit** | The library that turns `ITrackifyApi` into an HTTP client from its attributes |
| **SignalR** | The ASP.NET Core real-time messaging library carrying speed/LED commands and `SpeedChanged` broadcasts |
| **Spectre.Console** | The library behind the CLI's commands, tables and rendering |
| **SharpBrick.PoweredUp** | The .NET library implementing LWP; `.Mobile` (Plugin.BLE) and `.WinRT` variants supply the app transports |
| **Uno Platform** | The cross-platform XAML framework behind the app's five heads |
| **Debounce** | Delaying a send (200 ms here) and cancelling superseded ones, so a dragged slider produces one command per hub instead of dozens |
| **MCP** | Model Context Protocol — a planned, not-yet-implemented interface ([ADR-16](09-architecture-decisions.md#adr-16-mcp-server-to-live-in-infrastructure)) |

## Recurring abbreviations

| Short | Long |
|---|---|
| **CI** | Continuous Integration (GitHub Actions) |
| **CWE** | Common Weakness Enumeration — referenced in the security notes (CWE-117 log forging, CWE-209 error disclosure, CWE-942 permissive CORS) |
| **DI** | Dependency Injection (`Microsoft.Extensions.DependencyInjection`) |
| **MVVM** | Model–View–ViewModel, via CommunityToolkit.Mvvm |
| **Pi** | Raspberry Pi — the reference headless deployment target |
