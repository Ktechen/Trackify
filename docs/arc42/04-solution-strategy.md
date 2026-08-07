# 4. Solution Strategy

Five decisions explain most of the codebase. Everything else follows from them.

## 4.1 Clean Architecture with per-layer DI

**Problem.** Two very different front-ends (a five-head Uno app, a Spectre CLI) must behave
identically, and the maintainer is one person who cannot manually police layering.

**Approach.** Dependencies point inward — `Domain ← Application ← Infrastructure ← front-ends` — and
**each layer owns its own registration**: `AddTrackifyDomain()`, `AddTrackifyApplication()`,
`AddTrackifyInfrastructure(storePath?)`. A composition root chains them; it never reaches inside a
layer to register individual services.

```csharp
// Trackify.Cli/Program.cs — and, near-identically, App.xaml.cs and TrackifyServer.cs
services.AddTrackifyDomain();
services.AddTrackifyApplication();
services.AddTrackifyInfrastructure(storePath);
```

The rule is checked by NetArchTest in CI, so a wrong `using` fails the build rather than a review.
→ [ADR-01](09-architecture-decisions.md#adr-01-clean-architecture-with-per-layer-di-entry-points),
[ADR-07](09-architecture-decisions.md#adr-07-front-ends-see-traindto-never-domain-entities)

## 4.2 One BLE seam, four transports, selected at composition time

**Problem.** There is no portable .NET BLE API (TC-2). Android, iOS, Windows and Linux each need a
different stack, and some of those stacks only *compile* on their own target framework.

**Approach.** A single port, `ILegoService`, with six methods (`IsSupported`, `DiscoverAsync`,
`ConnectAsync`, `DisconnectAsync`, `SetSpeedAsync`, `SetLedAsync`). Everything above it — including
the whole UI and the whole CLI — depends only on the interface. Transport selection is a DI concern,
and it uses **two deliberately different mechanisms**:

- **Runtime check** for Linux: `AddLinuxLego()` tests `OperatingSystem.IsLinux()`. The BlueZ types
  compile everywhere, so one artifact can ship to the Pi *and* run (as a no-op) elsewhere.
- **Compile-time `#if`** for Android/iOS/Windows: `DirectLegoService` and `WindowsLegoService`
  reference SDK packages that only exist on those TFMs. A runtime check is impossible — the code
  would not compile.

→ [ADR-03](09-architecture-decisions.md#adr-03-ilegoservice-as-the-single-ble-seam),
[ADR-04](09-architecture-decisions.md#adr-04-two-transport-selection-styles-runtime-vs-compile-time)

## 4.3 Two front-ends, one control service

**Problem.** "Drive a train" means the same thing on a phone and on a Pi, but the two front-ends
share no UI framework.

**Approach.** `TrainControlService` implements the control use-case once, over a pure `TrainDto`. It
resolves the hub key (`HubId`, falling back to `BleAddress`), maps the configured `LedColorType` to
RGB through `LegoinoCatalog`, clamps speed to ±100, and debounces a dragged slider so only the final
value reaches the hardware. It is UI-neutral by contract: failures surface as exceptions or no-ops,
never as localized status text.

The Uno app adds presentation on top (MVVM, the SVG speed-profile graph, German labels); the CLI adds
argv parsing and Spectre rendering. Neither re-implements control logic.

## 4.4 Local-first, with an optional LAN backend

**Problem.** The device you *want* to hold (a phone, a browser tab) is not always the device with a
usable radio near the layout — and desktop/WASM heads have no BLE at all.

**Approach.** The app is local-first: by default the device drives the hubs itself. When you switch
to **Server mode** and give it a URL, a `SwitchingLegoService` routes every `ILegoService` call to a
`RemoteLegoService` instead. The remote transport uses **REST for one-shot actions** (Refit) and
**SignalR for real-time speed/LED**, and the server implements those endpoints by calling the *same*
`ILegoService` — so nothing about the control logic differs over the network.

Because the switch sits behind the same interface, toggling mode takes effect immediately, with no
restart and no UI changes.

→ [ADR-08](09-architecture-decisions.md#adr-08-rest-for-one-shot-actions-signalr-for-real-time-control),
[ADR-09](09-architecture-decisions.md#adr-09-runtime-directserver-switch-instead-of-a-restart)

## 4.5 Make the conventions executable

**Problem.** A hobby project's architecture erodes between sessions.

**Approach.** Every rule that *can* be enforced, is:

| Rule | Enforced by |
|---|---|
| Layer dependencies, DTO boundary | `LayerTrainDependencyTests` (NetArchTest) in CI |
| Namespace = folder, file-scoped namespaces | `IDE0130`/`IDE0161` as **errors** |
| Any warning at all | `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` |
| Package version drift | Central Package Management (`Directory.Packages.props`) |
| Uno version drift | `Uno.Sdk` pinned in `global.json`, not in package props |
| Bugs, coverage, security rating | SonarCloud (`sonar.yml`) + CodeQL, per PR |

What *cannot* be enforced automatically is stated as a convention in
[§2.4–2.5](02-architecture-constraints.md#24-self-imposed-conventions-enforced-by-the-build) and
verified in review.

## 4.6 Quality goals → strategy, at a glance

| Quality goal ([§1.2](01-introduction-and-goals.md#12-quality-goals)) | Strategic answer |
|---|---|
| Control responsiveness | Debounced immediate send (200 ms) locally; SignalR push, not polling, remotely; no round-trip in the local path |
| Portability across front-ends | `ILegoService` seam + shared `TrainControlService` + per-layer DI |
| Changeability / integrity | Clean Architecture, arch tests in CI, warnings-as-errors, DTO boundary |
| Unattended reliability | `trackify auto` re-reads config and reconnects each sweep; bounded connect retry; systemd `Restart=on-failure` |
| Operational safety | Cooperative cancellation on SIGINT: stop motors first, then disconnect; global exception handlers in all three hosts |
