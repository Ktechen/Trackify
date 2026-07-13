# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

**Trackify** is an [Uno Platform](https://platform.uno) app (single project, multi-head) that configures and controls **LEGO Powered Up** train hubs over **Bluetooth LE**, directly on-device — there is **no server/backend**. BLE talks the [LEGO Wireless Protocol (LWP) v3](https://lego.github.io/lego-ble-wireless-protocol-docs/) via [SharpBrick.PoweredUp](https://github.com/sharpbrick/powered-up).

- .NET 10, Uno.Sdk `6.5.36` (pinned in `global.json` — update Uno there, not in package props).
- Heads: `net10.0-android`, `net10.0-ios`, `net10.0-browserwasm`, `net10.0-desktop`, `net10.0-windows10.0.19041.0`.
- Central Package Management (`Directory.Packages.props`) — add versions there, reference without a version in the csproj.
- `TreatWarningsAsErrors=true` and `EnforceCodeStyleInBuild=true` are on. **The build fails on warnings and on code-style violations.**

## Build & verify

```bash
# Build one head (do this per head when verifying a change)
dotnet build Trackify/Trackify.csproj -f net10.0-desktop

# The four heads that build in this environment (iOS needs a Mac):
#   net10.0-android  net10.0-desktop  net10.0-browserwasm  net10.0-windows10.0.19041.0

# Desktop launch smoke test (no XAML/DI exceptions on startup)
dotnet run --project Trackify/Trackify.csproj -f net10.0-desktop
```

**The Uno Skia-rendered UI cannot be screenshotted in this environment** (GPU surface comes back blank; the WASM canvas render-loop times out the browser tool). So UI work cannot be pixel-verified here. Verification is: **build green on all four buildable heads + desktop launch smoke test**; the user confirms visuals on a real device.

## Architecture

- **`Presentation/`** — MVVM, folder-per-concern (see conventions). No business logic in pages.
- **`Services/`** — `ILegoService` abstraction with a per-platform implementation, chosen at DI time in `App.xaml.cs` → `RegisterLegoService`:
  - Android/iOS → `DirectLegoService` (SharpBrick `.Mobile` / Plugin.BLE)
  - Windows → `WindowsLegoService` (SharpBrick `.WinRT`)
  - other heads (desktop/wasm) → `UnsupportedLegoService` (no BLE stack; returns a clear "unsupported")
  - `LwpProtocol.cs` sends raw LWP commands (StartPower, SetRgbColor, connect-with-retry) — this is deliberately at the protocol layer rather than SharpBrick's typed device model, which is more robust because it doesn't require device knowledge to be populated first.
  - `NativeBluetooth.cs` — Android/iOS permission + device-info glue.
- **`Models/Trains/`** — `Train`, `TrackSegment` (both `ObservableObject`), enums, `LegoinoCatalog` (option lists).
- **`Helpers/SpeedCurve.cs`** — builds the `SpeedProfileGraph` (path data) used by the speed-profile widget.
- **Navigation & DI** — Uno.Extensions Hosting/Navigation. Routes/ViewMaps in `App.xaml.cs` → `RegisterRoutes`.

## Conventions (enforced)

- **Namespace must match folder** (`IDE0130` = error) and be **file-scoped** (`IDE0161` = error). `Platforms/**` is exempt (Uno convention namespaces like `Trackify.Droid`). Other code-style rules are downgraded to suggestions so they don't flood the build.
- **Type-name suffix per folder:** `Services/` → `*Service`, `Presentation/ViewModels/` → `*ViewModel`, `Presentation/Behaviors/` → `*Behavior`, `Presentation/Widgets/` → `*Widget`.
- **Folder is `Pages`, not "Screens".**
- **`Components/`** = page-specific composed sections that inherit the page's `DataContext`. **`Widgets/`** = reusable atoms that expose `DependencyProperty`s (e.g. `SpeedProfileWidget.Graph`).
- **No page code-behind** beyond `InitializeComponent()`, except the deliberate responsive master-detail layout in `Pages/MainPage.xaml.cs`. Use attached behaviors (e.g. `Behaviors/TappedCommandBehavior`) instead of `*_Tapped` handlers.
- **MVVM** with CommunityToolkit.Mvvm: field-based `[ObservableProperty]` and `[RelayCommand]`. (`MVVMTK0045` partial-property advice is intentionally suppressed.)
- Converters are registered **once** globally in `Styles/Converters.xaml` (merged in `App.xaml`); don't re-declare per page. Design tokens/styles live in `Styles/DesignTokens.xaml`.
- Classic `{Binding}` views carry a design-time `d:DataContext="{d:DesignInstance ...}"` (with `mc:Ignorable="d"`) so binding paths resolve in the IDE. This is design-time only. Add one when creating a new view/data-template.
- UI language / labels are **German**.

## Gotchas / hard-won facts

- **`Plugin.BLE` is pinned to `3.0.0`** — it must match the version `SharpBrick.PoweredUp.Mobile 5.0.2` was compiled against. A newer Plugin.BLE changes signatures SharpBrick calls → runtime `MissingMethodException` on connect. Its `net7.0-android33.0` asset is consumable by `net10.0-android`.
- **Connect uses a bounded retry** catching `NullReferenceException`/`ArgumentNullException` — works around an unfixed null-deref in SharpBrick's `BluetoothKernel.ConnectAsync` ([sharpbrick/powered-up#188](https://github.com/sharpbrick/powered-up/issues/188)).
- **`CS7064`** (wasm favicon) is kept as a warning (not error) via `WarningsNotAsErrors` — Uno.Resizetizer can generate `favicon.ico` after the compiler first references it on clean builds.
- Mobile safe-area (status bar / gesture nav / notch) is handled with the Uno `Toolkit` feature: `utu:SafeArea.Insets` on the page header (`Top`) and content (`Bottom`). Adding a mobile screen? Apply the same so system bars don't overlap.
